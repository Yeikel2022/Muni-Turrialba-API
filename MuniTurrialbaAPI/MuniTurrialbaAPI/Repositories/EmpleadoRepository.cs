using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public class EmpleadoRepository : IEmpleadoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:
        int resultado_empleado_crear;
        int resultado_empleado_actualizar;
        int resultado_empleado_eliminar;


        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public EmpleadoRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Estos métodos sirven para crear, actualizar o eliminar a un empleado(a) dentro -
         * de la base de datos. */
        public async Task<bool?> CrearEmpleado(EmpleadoCreateDto empleadoDto, int idUsuarioParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarEmpleado_ParaCrear(idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_empleado_crear es igual a 0, quiere decir que ese empleado(a) -
             * no existe en la base de datos.
             * 
             * Por lo que entonces si se podria crear ese empleado(a) en la base de datos -
             * respectivamente. */
            if (resultado != false && resultado_empleado_crear == 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var nuevoEmpleado = await conexionBD.ExecuteAsync("PROCED_CrearEmpleados",
                        //Se coloca los parametros:
                        new
                        {
                            Activo = empleadoDto.Activo,
                            Departamento = empleadoDto.Departamento,
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo crear el empleado(a), y de ese -
                     * modo mandar un error respectivamente. */
                    var respuestaEmpleado = nuevoEmpleado.ToString();
                    if (respuestaEmpleado == null)
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
                    string lugarOcurrido = "CrearEmpleado";

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

        public async Task<bool?> ActualizarEmpleado(EmpleadoCreateDto empleadoDto, int idUsuarioParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarEmpleado_ParaActualizar(idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_empleado_actualizar es diferente a 0, quiere decir que ese -
             * empleado(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese empleado(a) en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_empleado_actualizar != 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_Empleado",
                        //Se coloca los parametros:
                        new
                        {
                            Activo = empleadoDto.Activo,
                            Departamento = empleadoDto.Departamento,
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar el empleado(a), y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaEmpleado = resultadoActualizacion;
                    if (respuestaEmpleado == 0)
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
                    string lugarOcurrido = "ActualizarEmpleado";

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

        public async Task<bool?> EliminarEmpleado(int idUsuarioParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarEmpleado_ParaEliminar(idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_empleado_eliminar es diferente a 0, quiere decir que ese empleado(a) -
             * si existe en la base de datos.
             * 
             * Por lo que entonces si se podria eliminar ese empleado(a) en la base de datos -
             * respectivamente. */
            if (resultado != false && resultado_empleado_eliminar != 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync("PROCED_Eliminar_Empleado",
                        //Se coloca los parametros:
                        new
                        {
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo eliminar el empleado(a), y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaEmpleado = resultadoEliminacion;
                    if (respuestaEmpleado == 0)
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
                    string lugarOcurrido = "EliminarEmpleado";

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



        /* Estos métodos sirven para obtener a los empleados(as) que están dentro de la -
         * base de datos respectivamente. */
        public async Task<IEnumerable<ExtensionEmpleadoUsuarioEntitie>?> ObtenerEmpleados()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var empleados = await conexionBD.QueryAsync<ExtensionEmpleadoUsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarEmpleados", commandType:
                    System.Data.CommandType.StoredProcedure);
                
                
                /* Se cierra la conexión por temas de buenas prácticas y se devuelve todos -
                 * los empleados(as) como respuesta.*/
                conexionBD.Close();

                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                return empleados;
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
                string lugarOcurrido = "ObtenerEmpleados";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public async Task<EmpleadoEntitie?>? ObtenerEmpleado_PorIdUsuario(int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var empleadoObtenido = await conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    "PROCED_Consultar_Empleado_X_Usuario",
                    //Se coloca los parametros:
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda los datos del empleado que trae el procedimiento almacenado. */
                EmpleadoEntitie? empleado = empleadoObtenido;


                /* Si el empleado(a) que se obtuvo desde la BD es igual a nulo, significa -
                 * que no existe ese empleado(a), por lo que en este caso se cierra la -
                 * conexión de la base de datos y se envia un nulo como respuesta -
                 * respectivamente. */
                if (empleado == null)
                {
                    //Para cerrar la conexión, esto por temas de buenas prácticas.
                    conexionBD.Close();

                    return null;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Datos del empleado(a): " + empleado);
                 * 
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */
                
                //Para cerrar la conexión, esto por temas de buenas prácticas.
                conexionBD.Close();

                return empleadoObtenido;
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
                string lugarOcurrido = "ObtenerEmpleado_PorIdUsuario";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Estos métodos sirven para verificar si esos empleados(as) están o no en la base de datos.
         * Y ya con eso, dirigirlos a sus respectivos métodos, como son el de crear, actualizar o -
         * eliminar un empleado(a) respectivamente. */
        public bool? VerificarEmpleado_ParaCrear(int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_empleado_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var empleadoObtenido = conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarEmpleado",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? empleadoId = empleadoObtenido.Result?.Id.ToString();


                /* Si el ID del empleado(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese empleado(a), por lo que en este caso se guarda ese ID y se -
                 * envia un falso al método: CrearEmpleado. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe ese -
                 * empleado(a) en la base de datos, y, en consecuencia, no permita la creación de -
                 * ese dato respectivamente. */
                if (empleadoId != null)
                {
                    resultado_empleado_crear = empleadoObtenido.Result!.Id;

                    conexionBD.Close();

                    return false;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver la variable empleadoId: " + empleadoId);
                 * 
                 * Debug.WriteLine("Para ver la variable empleadoObtenido: " + empleadoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable empleadoObtenido: " + empleadoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                conexionBD.Close();

                return true;
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

                string lugarOcurrido = "VerificarEmpleado_ParaCrear";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarEmpleado_ParaActualizar(int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_empleado_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var empleadoObtenido = conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarEmpleado",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? empleadoId = empleadoObtenido.Result?.Id.ToString();


                /* Si el ID del empleado(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese empleado(a), por lo que en este caso se guarda ese ID y se -
                 * envia un true al método: ActualizarEmpleado. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * empleado(a) en la base de datos, y, en consecuencia, pueda permitir la actualización -
                 * de ese dato respectivamente. */
                if (empleadoId != null)
                {
                    resultado_empleado_actualizar = empleadoObtenido.Result!.Id_Usuario;

                    conexionBD.Close();
                    
                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable empleadoId: " + empleadoId);
                 * 
                 * Debug.WriteLine("Para ver la variable empleadoObtenido: " + empleadoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable empleadoObtenido: " + empleadoObtenido);
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

                string lugarOcurrido = "VerificarEmpleado_ParaActualizar";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarEmpleado_ParaEliminar(int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_empleado_eliminar = 0;

                //Para ejecutar el procedimiento almacenado.
                var empleadoObtenido = conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarEmpleado",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? empleadoId = empleadoObtenido.Result?.Id.ToString();
                Task<EmpleadoEntitie?> empleado = empleadoObtenido;


                /* Si el ID del empleado(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese empleado(a), por lo que en este caso se guarda ese ID y se -
                 * envia un true al método: EliminarEmpleado. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * empleado(a) en la base de datos, y, en consecuencia, pueda permitir la eliminación -
                 * de ese dato respectivamente. */
                if (empleadoId != null)
                {
                    resultado_empleado_eliminar = empleadoObtenido.Result!.Id_Usuario;
                    
                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable empleadoId: " + empleadoId);
                 * Debug.WriteLine("Para ver la variable empleado: " + empleado);
                 * 
                 * Debug.WriteLine("Para ver la variable empleadoObtenido: " + empleadoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable empleadoObtenido: " + empleadoObtenido);
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

                string lugarOcurrido = "VerificarEmpleado_ParaEliminar";

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
