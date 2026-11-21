using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public class RegistroArchivoRepository : IRegistroArchivoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:
        int resultado_archivo_actualizar;
        int resultado_archivo_eliminar;
        int resultado_archivo_compartir;


        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public RegistroArchivoRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Estos métodos sirven para crear, actualizar, eliminar o compartir un registro de -
         * archivo dentro de la base de datos. */
        public async Task<bool?> CrearRegistrosArchivos(RegistroArchivoCreateDto archivoDto, int idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                /* Aqui lo que se hace es decodificar el archivo que se paso por parametro -
                 * a un arreglo de bytes. Esto porque en la base de datos se maneja el tipo - 
                 * Varbinary y por eso ocupa si o si ese arreglo de bytes. 
                 *
                 * Además que en la documentación de Microsoft mencionan que con este comando: -
                 * "Convert.FromBase64String(archivoDto.Archivo)", nos permitira devolver un -
                 * arreglo (o una lista) de bytes respectivamente. */
                byte[] archivoParseado = Convert.FromBase64String(archivoDto.Archivo);

                /* Aqui lo que se hace es simplemente tomar la fecha y hora de subida al momento -
                 * que se esta usando este método. */
                DateTime Fecha_Subida = DateTime.Now;

                //Para ejecutar el procedimiento almacenado.
                var nuevoRegistroArchivo = await conexionBD.ExecuteAsync("PROCED_CrearRegistroArchivos",
                    //Se coloca los parametros:
                    new
                    {
                        Archivo = archivoParseado,
                        Nombre_Archivo = archivoDto.Nombre_Archivo,
                        Formato_Archivo = archivoDto.Formato_Archivo,
                        Fecha_Subida = Fecha_Subida,
                        Nombre_Originario = archivoDto.Nombre_Originario,
                        Apellido_1_Originario = archivoDto.Apellido_1_Originario,
                        Apellido_2_Originario = archivoDto.Apellido_2_Originario,
                        Correo_Electronico_Originario = archivoDto.Correo_Electronico_Originario,
                        Departamento_Originario = archivoDto.Departamento_Originario,
                        Nombre_Destinatario = archivoDto.Nombre_Destinatario,
                        Apellido_1_Destinatario = archivoDto.Apellido_1_Destinatario,
                        Apellido_2_Destinatario = archivoDto.Apellido_2_Destinatario,
                        Correo_Electronico_Destinatario = archivoDto.Correo_Electronico_Destinatario,
                        Departamento_Destinatario = archivoDto.Departamento_Destinatario,
                        Estado_Archivo = archivoDto.Estado_Archivo,
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                 * para saber si realmente no pudo crear el registro del archivo, -
                 * y de ese modo mandar un error respectivamente. */
                var respuestaRegistroArchivo = nuevoRegistroArchivo.ToString();
                if (respuestaRegistroArchivo == null)
                {
                    return null;
                }


                /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                 * true como respuesta.*/
                conexionBD.Close();

                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                return true;
            }
            catch (Exception error)
            {
                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("No se pudo realizar correctamente la operación, " + 
                 * "esto por el siguiente error: " + error); */


                /* Esta validación sirve para evitar un error de tipo: NullException en -
                 * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                 * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                 * el porque se pone esta validación.
                 * 
                 * Si en dado caso el error no tiene que ver con la conexión de la BD, entonces -
                 * si se podria entrar para cerrar la conexión respectivamente. */
                if (conexionBD != null)
                {
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                }

                //Se coloca el nombre del método en que ocurrio el error.
                string lugarOcurrido = "CrearRegistrosArchivos";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public async Task<bool?> ActualizarRegistrosArchivos(RegistroArchivoCreateDto archivoDto, int idEmpleadoActualParametrizado, int idEmpleadoNuevoParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarRegistrosArchivo_ParaActualizar(idEmpleadoActualParametrizado, archivoDto.Fecha_Subida);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_archivo_actualizar es diferente a 0, quiere decir que ese -
             * registro de archivo de ese empleado(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese registro de archivo -
             * en la base de datos respectivamente. */
            if (resultado != false && resultado_archivo_actualizar != 0)
            {
                try
                {
                    /* Aqui lo que se hace es guardar el ID del registro de archivo de ese -
                     * empleado(a) que se quiere actualizar respectivamente. */
                    int Id_Registro_Archivo = resultado_archivo_actualizar;

                    /* Aqui lo que se hace es guardar el ID del nuevo empleado(a), esto debido -
                     * a que existe una tabla donde usa ese id y del id del registro de archivo, -
                     * por lo que es importante poner ese nuevo id en esa tabla, para que la -
                     * actualización se realize correctamente.
                     *
                     * Si en dado caso se colocara el id del empleado actual (el cual se usa para -
                     * tener el id del registro de archivo que tiene asociado), entonces lo que -
                     * pasaria seria que esa tabla no se estaría actualizando correctamente, lo -
                     * cual puede ser un gran problema a la hora de querer colocar otro destinatario(a) -
                     * respectivamente. */
                    int Id_Empleado_Nuevo = idEmpleadoNuevoParametrizado;

                   /* Aqui lo que se hace es decodificar el archivo que se paso por parametro -
                    * a un arreglo de bytes. Esto porque en la base de datos se maneja el tipo - 
                    * Varbinary y por eso ocupa si o si ese arreglo de bytes. 
                    *
                    * Además que en la documentación de Microsoft mencionan que con este comando: -
                    * "Convert.FromBase64String(archivoDto.Archivo)", nos permitira devolver un -
                    * arreglo (o una lista) de bytes respectivamente. */
                    byte[] archivoParseado = Convert.FromBase64String(archivoDto.Archivo);

                    /* Aqui lo que se hace es simplemente tomar la fecha y hora de actualización -
                     * al momento que se esta usando este método. */
                    DateTime Fecha_Actualizacion = DateTime.Now;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_RegistroArchivo",
                        //Se coloca los parametros:
                        new
                        {
                            Archivo = archivoParseado,
                            Nombre_Archivo = archivoDto.Nombre_Archivo,
                            Formato_Archivo = archivoDto.Formato_Archivo,
                            Fecha_Actualizacion = Fecha_Actualizacion,
                            Nombre_Destinatario = archivoDto.Nombre_Destinatario,
                            Apellido_1_Destinatario = archivoDto.Apellido_1_Destinatario,
                            Apellido_2_Destinatario = archivoDto.Apellido_2_Destinatario,
                            Correo_Electronico_Destinatario = archivoDto.Correo_Electronico_Destinatario,
                            Departamento_Destinatario = archivoDto.Departamento_Destinatario,
                            Id_Registro_Archivo = Id_Registro_Archivo,
                            Id_Empleado = Id_Empleado_Nuevo
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar el registro de archivo, -
                     * y de ese modo mandar un error respectivamente. */
                    var respuestaRegistroArchivo = resultadoActualizacion;
                    if (respuestaRegistroArchivo == 0)
                    {
                        return null;
                    }


                    /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                     * true como respuesta.*/
                    conexionBD.Close();

                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                     * Debug.WriteLine(resultadoActualizacion.ToString()); */

                    return true;
                }
                catch (Exception error)
                {
                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("No se pudo realizar correctamente la operación, " + 
                     * "esto por el siguiente error: " + error); */


                    /* Esta validación sirve para evitar un error de tipo: NullException en -
                     * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                     * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                     * el porque se pone esta validación.
                     * 
                     * Si en dado caso el error no tiene que ver con la conexión de la BD, entonces -
                     * si se podria entrar para cerrar la conexión respectivamente. */
                    if (conexionBD != null)
                    {
                        //Se cierra la conexión por temas de buenas prácticas.
                        conexionBD.Close();
                    }

                    //Se coloca el nombre del método en que ocurrio el error.
                    string lugarOcurrido = "ActualizarRegistrosArchivos";

                    //Se manda a registrar el error que acaba de ocurrir en la BD.
                    RegistrarError(error, lugarOcurrido);

                    return null;
                }
            }


            /* Comandos para poder ver el resultado en las variables en un debug:
             * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
             * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

            //Se cierra la conexión por temas de buenas prácticas.
            conexionBD.Close();

            return false;
        }

        public async Task<bool?> ActualizarEstadoRegistroArchivo(string estadoArchivoParametrizado, string fechaSubidaParametrizada, int idEmpleadoParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarRegistrosArchivo_ParaActualizar(idEmpleadoParametrizado, fechaSubidaParametrizada);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_archivo_actualizar es diferente a 0, quiere decir que ese -
             * registro de archivo de ese empleado(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese registro de archivo -
             * en la base de datos respectivamente. */
            if (resultado != false && resultado_archivo_actualizar != 0)
            {
                try
                {
                    /* Aqui lo que se hace es guardar el ID del registro de archivo de ese -
                     * empleado(a) que se quiere actualizar respectivamente. */
                    int Id_Registro_Archivo = resultado_archivo_actualizar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_EstadoArchivo_RegistroArchivos",
                        //Se coloca los parametros:
                        new
                        {
                            Estado_Archivo = estadoArchivoParametrizado,
                            Id_Registro_Archivo = Id_Registro_Archivo
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar el registro de archivo, -
                     * y de ese modo mandar un error respectivamente. */
                    var respuestaRegistroArchivo = resultadoActualizacion;
                    if (respuestaRegistroArchivo == 0)
                    {
                        return null;
                    }


                    /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                     * true como respuesta.*/
                    conexionBD.Close();

                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                     * Debug.WriteLine(resultadoActualizacion.ToString()); */

                    return true;
                }
                catch (Exception error)
                {
                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("No se pudo realizar correctamente la operación, " + 
                     * "esto por el siguiente error: " + error); */


                    /* Esta validación sirve para evitar un error de tipo: NullException en -
                     * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                     * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                     * el porque se pone esta validación.
                     * 
                     * Si en dado caso el error no tiene que ver con la conexión de la BD, entonces -
                     * si se podria entrar para cerrar la conexión respectivamente. */
                    if (conexionBD != null)
                    {
                        //Se cierra la conexión por temas de buenas prácticas.
                        conexionBD.Close();
                    }

                    //Se coloca el nombre del método en que ocurrio el error.
                    string lugarOcurrido = "ActualizarEstadoRegistroArchivo";

                    //Se manda a registrar el error que acaba de ocurrir en la BD.
                    RegistrarError(error, lugarOcurrido);

                    return null;
                }
            }


            /* Comandos para poder ver el resultado en las variables en un debug:
             * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
             * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

            //Se cierra la conexión por temas de buenas prácticas.
            conexionBD.Close();

            return false;
        }

        public async Task<bool?> EliminarRegistrosArchivos(string fechaSubidaParametrizada, int idEmpleadoParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarRegistrosArchivo_ParaEliminar(idEmpleadoParametrizado, fechaSubidaParametrizada);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_archivo_eliminar es diferente a 0, quiere decir que ese -
             * registro de archivo de ese empleado(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria eliminar ese registro de archivo en -
             * la base de datos respectivamente. */
            if (resultado != false && resultado_archivo_eliminar != 0)
            {
                try
                {
                    /* Aqui lo que se hace es guardar el ID del registro de archivo de ese -
                     * empleado(a) que se quiere eliminar respectivamente. */
                    int Id_Registro_Archivo = resultado_archivo_eliminar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync("PROCED_Eliminar_RegistroArchivo",
                        //Se coloca los parametros:
                        new
                        {
                            Id_Registro_Archivo = Id_Registro_Archivo
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo eliminar el registro de archivo, -
                     * y de ese modo mandar un error respectivamente. */
                    var respuestaRegistroArchivo = resultadoEliminacion;
                    if (respuestaRegistroArchivo == 0)
                    {
                        return null;
                    }

                    /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                     * true como respuesta.*/
                    conexionBD.Close();

                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                     * Debug.WriteLine(resultadoEliminacion.ToString()); */

                    return true;
                }
                catch (Exception error)
                {
                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error); */


                    /* Esta validación sirve para evitar un error de tipo: NullException en -
                     * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                     * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                     * el porque se pone esta validación respectivamente. */
                    if (conexionBD != null)
                    {
                        //Se cierra la conexión por temas de buenas prácticas.
                        conexionBD.Close();
                    }

                    //Se coloca el nombre del método en que ocurrio el error.
                    string lugarOcurrido = "EliminarRegistrosArchivos";

                    //Se manda a registrar el error que acaba de ocurrir en la BD.
                    RegistrarError(error, lugarOcurrido);

                    return null;
                }
            }


            /* Comandos para poder ver el resultado en las variables en un debug:
             * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
             * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

            //Se cierra la conexión por temas de buenas prácticas.
            conexionBD.Close();

            return false;
        }

        public async Task<bool?> CompartirRegistrosArchivos(RegistroArchivoCreateDto archivoDto, int idEmpleadoParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarRegistrosArchivo_ParaCompartir(idEmpleadoParametrizado, archivoDto.Fecha_Subida);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_archivo_compartir es diferente a 0, quiere decir que ese -
             * registro de archivo de ese empleado(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria compartir ese registro de archivo en -
             * la base de datos respectivamente. */
            if (resultado != false && resultado_archivo_compartir != 0)
            {
                try
                {
                    /* Aqui lo que se hace es guardar el ID del registro de archivo -
                     * de ese empleado(a) que se quiere compartir respectivamente. */
                    int Id_Registro_Archivo = resultado_archivo_compartir;

                    /* Aqui lo que se hace es simplemente tomar la fecha y hora de -
                     * envio al momento que se esta usando este método. */
                    DateTime Fecha_Envio = DateTime.Now;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoCompartir = await conexionBD.ExecuteAsync("PROCED_Compartir_RegistroArchivos",
                        //Se coloca los parametros:
                        new
                        {
                            Estado_Archivo = archivoDto.Estado_Archivo,
                            Fecha_Envio = Fecha_Envio,
                            Id_Registro_Archivo = Id_Registro_Archivo
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo compartir el registro de archivo, -
                     * y de ese modo mandar un error respectivamente. */
                    var respuestaRegistroArchivo = resultadoCompartir;
                    if (respuestaRegistroArchivo == 0)
                    {
                        return null;
                    }

                    /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                     * true como respuesta.*/
                    conexionBD.Close();

                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                     * Debug.WriteLine(resultadoEliminacion.ToString()); */

                    return true;
                }
                catch (Exception error)
                {
                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error); */


                    /* Esta validación sirve para evitar un error de tipo: NullException en -
                     * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                     * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                     * el porque se pone esta validación.
                     * 
                     * Si en dado caso el error no tiene que ver con la conexión de la BD, entonces -
                     * si se podria entrar para cerrar la conexión respectivamente. */
                    if (conexionBD != null)
                    {
                        //Se cierra la conexión por temas de buenas prácticas.
                        conexionBD.Close();
                    }

                    //Se coloca el nombre del método en que ocurrio el error.
                    string lugarOcurrido = "CompartirRegistrosArchivos";

                    //Se manda a registrar el error que acaba de ocurrir en la BD.
                    RegistrarError(error, lugarOcurrido);

                    return null;
                }
            }


            /* Comandos para poder ver el resultado en las variables en un debug:
             * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
             * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

            //Se cierra la conexión por temas de buenas prácticas.
            conexionBD.Close();

            return false;
        }



        /* Estos métodos sirven para obtener a los registros de archivos que están dentro -
         * de la base de datos. */
        public async Task<IEnumerable<RegistroArchivoEntitie>?> ObtenerRegistrosArchivos()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var archivos = await conexionBD.QueryAsync<RegistroArchivoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarRegistroArchivos", commandType:
                    System.Data.CommandType.StoredProcedure);


                /* Se cierra la conexión por temas de buenas prácticas y se devuelve todos -
                 * los registros de archivos de los empleados(as) como respuesta.*/
                conexionBD.Close();

                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                return archivos;
            }
            catch (Exception error)
            {
                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error); */


                /* Esta validación sirve para evitar un error de tipo: NullException en -
                 * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                 * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                 * el porque se pone esta validación respectivamente. */
                if (conexionBD != null)
                {
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                }

                //Se coloca el nombre del método en que ocurrio el error.
                string lugarOcurrido = "ObtenerRegistrosArchivos";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public async Task<IEnumerable<RegistroArchivoEntitie>?> ObtenerRegistrosArchivosCompartidos(int idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var archivos = await conexionBD.QueryAsync<RegistroArchivoEntitie>(
                    "PROCED_Consultar_RegistroArchivoCompartido",
                    //Se coloca los parametros:
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda los datos de los registros de archivos que trae el procedimiento -
                 * almacenado. */
                string? verDatos = archivos.ToString();


                /* Si los registros de archivos que se obtuvieron desde la BD son iguales a -
                 * nulo o si la cantidad que tienen es igual a cero, eso significa que no -
                 * existen esos registros, por lo que en este caso se cierra la conexión -
                 * de la base de datos y se envia un nulo como respuesta respectivamente. */
                if (archivos?.ToString() == null || archivos.Count() == 0)
                {
                    //Para cerrar la conexión, esto por temas de buenas prácticas.
                    conexionBD.Close();

                    return null;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Datos de los archivos: " + verDatos);
                 * 
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                //Para cerrar la conexión, esto por temas de buenas prácticas.
                conexionBD.Close();

                return archivos;
            }
            catch (Exception error)
            {
                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error); */


                /* Esta validación sirve para evitar un error de tipo: NullException en -
                 * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                 * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                 * el porque se pone esta validación respectivamente. */
                if (conexionBD != null)
                {
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                }

                //Se coloca el nombre del método en que ocurrio el error.
                string lugarOcurrido = "ObtenerRegistrosArchivosCompartidos";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Estos métodos sirven para verificar si esos empleados(as) están o no en la base de datos.
         * Y ya con eso, dirigirlos a sus respectivos métodos, como son el de actualizar, eliminar -
         * o compartir un registro de archivo respectivamente. */
        public bool? VerificarRegistrosArchivo_ParaActualizar(int idEmpleadoParametrizado, string fechaSubidaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la fecha pasada por parametro es distinto de nulo, puede pasar.
            if (fechaSubidaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_archivo_actualizar = 0;

                /* Aqui lo que se esta haciendo es parsear la fecha de subida a -
                 * un tipo: DateTime, esto porque si se deja esa fecha como estaba -
                 * podria causar un error, ya que al intentar registrarlo en la base -
                 * de datos, el tipo string chocaria con el tipo DateTime que esta -
                 * definido, de ahi el porque se parsea respectivamente. */
                DateTime fechaSubida_Parseada = DateTime.Parse(fechaSubidaParametrizada);

                //Para ejecutar el procedimiento almacenado.
                var registroArchivoObtenido = conexionBD.QueryFirstOrDefaultAsync<RegistroArchivoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_VerificarRegistroArchivo",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado,
                        Fecha_Subida = fechaSubida_Parseada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? archivoId = registroArchivoObtenido.Result?.Id.ToString();


                /* Si el ID del archivo que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese registro de archivo con un empleado(a), por lo que en este -
                 * caso se guarda ese ID y se envia un true a los métodos: ActualizarRegistrosArchivos -
                 * y ActualizarEstadoRegistroArchivo. 
                 * 
                 * De modo que en dichos métodos puedan tener conocimiento de que si existe ese -
                 * archivo en la base de datos, y, en consecuencia, puedan permitir la actualización -
                 * de ese dato respectivamente. */
                if (archivoId != null)
                {
                    resultado_archivo_actualizar = registroArchivoObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable archivoId: " + archivoId);
                 * 
                 * Debug.WriteLine("Para ver la variable registroArchivoObtenido: " + registroArchivoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable registroArchivoObtenido: " + registroArchivoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                conexionBD.Close();

                return false;
            }
            catch (Exception error)
            {
                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("No se pudo realizar correctamente la operación, "
                 * + "esto por el siguiente error: " + error); */


                /* Esta validación sirve para evitar un error de tipo: NullException en -
                 * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                 * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                 * el porque se pone esta validación respectivamente. */
                if (conexionBD != null)
                {
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                }

                string lugarOcurrido = "VerificarRegistrosArchivo_ParaActualizar";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarRegistrosArchivo_ParaEliminar(int idEmpleadoParametrizado, string fechaSubidaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la fecha pasada por parametro es distinto de nulo, puede pasar.
            if (fechaSubidaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_archivo_eliminar = 0;

                /* Aqui lo que se esta haciendo es parsear la fecha de subida a -
                 * un tipo: DateTime, esto porque si se deja esa fecha como estaba -
                 * podria causar un error, ya que al intentar registrarlo en la base -
                 * de datos, el tipo string chocaria con el tipo DateTime que esta -
                 * definido, de ahi el porque se parsea respectivamente. */
                DateTime fechaSubida_Parseada = DateTime.Parse(fechaSubidaParametrizada);

                //Para ejecutar el procedimiento almacenado.
                var registroArchivoObtenido = conexionBD.QueryFirstOrDefaultAsync<RegistroArchivoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_VerificarRegistroArchivo",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado,
                        Fecha_Subida = fechaSubida_Parseada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? archivoId = registroArchivoObtenido.Result?.Id.ToString();
                Task<RegistroArchivoEntitie?> archivo = registroArchivoObtenido;


                /* Si el ID del archivo que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese registro de archivo con un empleado(a), por lo que en este -
                 * caso se guarda ese ID y se envia un true al método: EliminarRegistrosArchivos. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * archivo en la base de datos, y, en consecuencia, pueda permitir la eliminación -
                 * de ese dato respectivamente. */
                if (archivoId != null)
                {
                    resultado_archivo_eliminar = registroArchivoObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable archivoId: " + archivoId);
                 * Debug.WriteLine("Para ver la variable archivo: " + archivo);
                 * 
                 * Debug.WriteLine("Para ver la variable registroArchivoObtenido: " + registroArchivoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable registroArchivoObtenido: " + registroArchivoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                conexionBD.Close();

                return false;
            }
            catch (Exception error)
            {
                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error); */


                /* Esta validación sirve para evitar un error de tipo: NullException en -
                 * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                 * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                 * el porque se pone esta validación respectivamente. */
                if (conexionBD != null)
                {
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                }

                string lugarOcurrido = "VerificarRegistrosArchivo_ParaEliminar";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarRegistrosArchivo_ParaCompartir(int idEmpleadoParametrizado, string fechaSubidaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la fecha pasada por parametro es distinto de nulo, puede pasar.
            if (fechaSubidaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_archivo_compartir = 0;

                /* Aqui lo que se esta haciendo es parsear la fecha de subida a -
                 * un tipo: DateTime, esto porque si se deja esa fecha como estaba -
                 * podria causar un error, ya que al intentar registrarlo en la base -
                 * de datos, el tipo string chocaria con el tipo DateTime que esta -
                 * definido, de ahi el porque se parsea respectivamente. */
                DateTime fechaSubida_Parseada = DateTime.Parse(fechaSubidaParametrizada);

                //Para ejecutar el procedimiento almacenado.
                var registroArchivoObtenido = conexionBD.QueryFirstOrDefaultAsync<RegistroArchivoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_VerificarRegistroArchivo",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado,
                        Fecha_Subida = fechaSubida_Parseada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? archivoId = registroArchivoObtenido.Result?.Id.ToString();
                Task<RegistroArchivoEntitie?> archivo = registroArchivoObtenido;


                /* Si el ID del archivo que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese registro de archivo con un empleado(a), por lo que en este -
                 * caso se guarda ese ID y se envia un true al método: CompartirRegistrosArchivos. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * archivo en la base de datos, y, en consecuencia, pueda permitir compartir -
                 * ese dato respectivamente. */
                if (archivoId != null)
                {
                    resultado_archivo_compartir = registroArchivoObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable archivoId: " + archivoId);
                 * Debug.WriteLine("Para ver la variable archivo: " + archivo);
                 * 
                 * Debug.WriteLine("Para ver la variable registroArchivoObtenido: " + registroArchivoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable registroArchivoObtenido: " + registroArchivoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                conexionBD.Close();

                return false;
            }
            catch (Exception error)
            {
                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error); */


                /* Esta validación sirve para evitar un error de tipo: NullException en -
                 * la variable: conexionBD, esto porque puede pasar que la conexión de la BD -
                 * no pueda concretarse, y debido a eso daria un nulo como respuesta, de ahi -
                 * el porque se pone esta validación respectivamente. */
                if (conexionBD != null)
                {
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                }

                string lugarOcurrido = "VerificarRegistrosArchivo_ParaCompartir";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Este método sirve para registrar los errores que podrian suceder durante la ejecución -
         * de los demás métodos respectivamente. */
        private async void RegistrarError(Exception errorParametrizado, string lugarOcurridoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var nuevaConexionBD = CreateConnection();

            //Se crea una variable de tipo datetime para indicar la fecha en que ocurrio el error.
            DateTime fechaOcurrido = DateTime.Now;

            //Para ejecutar el procedimiento almacenado.
            var registrarError = await nuevaConexionBD.ExecuteAsync("PROCED_CrearRegistroErrores",
                //Se coloca los parametros:
                new
                {
                    Nombre = errorParametrizado.GetType().ToString(),
                    Descripcion = errorParametrizado.Message,
                    Lugar_Ocurrido = lugarOcurridoParametrizado,
                    Fecha_Ocurrido = fechaOcurrido
                },
                commandType: System.Data.CommandType.StoredProcedure);

            /* Comandos para poder ver el resultado en las variables en un debug:
             * Debug.WriteLine("No se pudo realizar correctamente la operación, esto por el siguiente error: " + errorParametrizado);
             * Debug.WriteLine("Para ver si la nueva conexión sigue activa: " + nuevaConexionBD.State);
             * Debug.WriteLine("Para ver si la nueva conexión se cerro: " + nuevaConexionBD.State); */

            //Se cierra la conexión por temas de buenas prácticas.
            nuevaConexionBD.Close();
        }


        //|========================================| FIN DE LA CLASE |========================================|
    }
}
