using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using System.Diagnostics;

namespace MuniTurrialbaAPI.Repositories
{
    public class SalarioRepository : ISalarioRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:
        int resultado_salario_crear;
        int resultado_salario_actualizar;
        int resultado_salario_eliminar;


        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public SalarioRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Estos métodos sirven para crear, actualizar o eliminar un salario dentro de la -
         * base de datos. */
        public async Task<bool?> CrearSalarios(SalarioCreateDto salarioDto, int idEmpleadoParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarSalario_ParaCrear(idEmpleadoParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Para ver lo que hay en resultado_salario_crear:
            Debug.WriteLine("Para ver resultado_salario_crear: " + resultado_salario_crear);

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_salario_crear es igual a 0, quiere decir que ese salario -
             * de ese empleado(a) no existe en la base de datos.
             * 
             * Por lo que entonces si se podria crear ese registro en la base de -
             * datos respectivamente. */
            if (resultado != false && resultado_salario_crear == 0)
            {
                try
                {
                    /* Aqui lo que se esta haciendo es parsear la fecha de entrega a -
                     * un tipo: DateTime, esto porque si se deja esa fecha como estaba -
                     * podria causar un error, esto porque al intentar registrarlo en -
                     * la base de datos, el tipo string chocaria con el tipo DateTime -
                     * que esta definido, de ahi el porque se parsea respectivamente. */
                    DateTime fechaEntrega_Parseado = DateTime.Parse(salarioDto.Fecha_Entrega);

                    //Para ejecutar el procedimiento almacenado.
                    var nuevoSalario = await conexionBD.ExecuteAsync("PROCED_CrearSalarios",
                        //Se coloca los parametros:
                        new
                        {
                            Fecha_Entrega = fechaEntrega_Parseado,
                            Salario = salarioDto.Salario,
                            Descripcion = salarioDto.Descripcion,
                            Id_Empleado = idEmpleadoParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo crear el salario, y de ese modo -
                     * mandar un error respectivamente. */
                    var respuestaSalario = nuevoSalario.ToString();
                    if (respuestaSalario == null)
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
                    string lugarOcurrido = "CrearSalarios";

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

        public async Task<bool?> ActualizarSalarios(SalarioCreateDto salarioDto, int idEmpleadoParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarSalario_ParaActualizar(idEmpleadoParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_salario_actualizar es diferente a 0, quiere decir que ese -
             * salario de ese empleado(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese registro en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_salario_actualizar != 0)
            {
                try
                {
                    /* Aqui lo que se esta haciendo es parsear la fecha de entrega a -
                     * un tipo: DateTime, esto porque si se deja esa fecha como estaba -
                     * podria causar un error, esto porque al intentar registrarlo en -
                     * la base de datos, el tipo string chocaria con el tipo DateTime -
                     * que esta definido, de ahi el porque se parsea respectivamente. */
                    DateTime fechaEntrega_Parseada = DateTime.Parse(salarioDto.Fecha_Entrega);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_Salario",
                        //Se coloca los parametros:
                        new
                        {
                            Fecha_Entrega = fechaEntrega_Parseada,
                            Salario = salarioDto.Salario,
                            Descripcion = salarioDto.Descripcion,
                            Id_Empleado = idEmpleadoParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar el salario, y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaSalario = resultadoActualizacion;
                    if (respuestaSalario == 0)
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
                    string lugarOcurrido = "ActualizarSalarios";

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

        public async Task<bool?> EliminarSalarios(int idEmpleadoParametrizado)
        {
            /* Es para saber si ese empleado(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese empleado(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarSalario_ParaEliminar(idEmpleadoParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_salario_eliminar es diferente a 0, quiere decir que ese -
             * salario de ese empleado(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria eliminar ese registro en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_salario_eliminar != 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync("PROCED_Eliminar_Salario",
                        //Se coloca los parametros:
                        new
                        {
                            Id_Empleado = idEmpleadoParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo eliminar el salario, y de ese -
                     * modo mandar un error respectivamente. */
                    var respuestaSalario = resultadoEliminacion;
                    if (respuestaSalario == 0)
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
                    string lugarOcurrido = "EliminarSalarios";

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



        /* Este método sirve para obtener todos los salarios de los empleados(as) dentro -
         * de la base de datos. */
        public async Task<IEnumerable<ExtensionSalarioEntitie>?> ObtenerSalarios()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var salarios = await conexionBD.QueryAsync<ExtensionSalarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarSalarios", commandType:
                    System.Data.CommandType.StoredProcedure);


                /* Se cierra la conexión por temas de buenas prácticas y se devuelve todos -
                 * los salarios de los empleados(as) como respuesta. */
                conexionBD.Close();

                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                return salarios;
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
                string lugarOcurrido = "ObtenerSalarios";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Estos métodos sirven para verificar si esos empleados(as) están o no en la base de datos.
         * Y ya con eso, dirigirlos a sus respectivos métodos, como son el de crear, actualizar o -
         * eliminar un salario respectivamente. */
        public bool? VerificarSalario_ParaCrear(int idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_salario_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var salarioObtenido = conexionBD.QueryFirstOrDefaultAsync<SalarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Salario_X_IdEmpleado",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? salarioId = salarioObtenido.Result?.Id.ToString();


                /* Si el ID del salario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese salario con un empleado(a), por lo que en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearSalarios. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe ese -
                 * salario en la base de datos, y, en consecuencia, no permita la creación de -
                 * ese dato respectivamente. */
                if (salarioId != null)
                {
                    resultado_salario_crear = salarioObtenido.Result!.Id;

                    conexionBD.Close();

                    return false;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver la variable salarioId: " + salarioId);
                 * 
                 * Debug.WriteLine("Para ver la variable salarioObtenido: " + salarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable salarioObtenido: " + salarioObtenido);
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

                string lugarOcurrido = "VerificarSalario_ParaCrear";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarSalario_ParaActualizar(int idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_salario_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var salarioObtenido = conexionBD.QueryFirstOrDefaultAsync<SalarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Salario_X_IdEmpleado",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado.*/
                string? salarioId = salarioObtenido.Result?.Id.ToString();


                /* Si el ID del salario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese salario con un empleado(a), por lo que en este caso se guarda -
                 * ese ID y se envia un true al método: ActualizarSalarios. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * salario en la base de datos, y, en consecuencia, pueda permitir la actualización -
                 * de ese dato respectivamente. */
                if (salarioId != null)
                {
                    resultado_salario_actualizar = salarioObtenido.Result!.Id_Empleado;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable salarioId: " + salarioId);
                 * 
                 * Debug.WriteLine("Para ver la variable salarioObtenido: " + salarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable salarioObtenido: " + salarioObtenido);
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

                string lugarOcurrido = "VerificarSalario_ParaActualizar";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarSalario_ParaEliminar(int idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_salario_eliminar = 0;

                //Para ejecutar el procedimiento almacenado.
                var salarioObtenido = conexionBD.QueryFirstOrDefaultAsync<SalarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Salario_X_IdEmpleado",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? salarioId = salarioObtenido.Result?.Id.ToString();
                Task<SalarioEntitie?> salario = salarioObtenido;


                /* Si el ID del salario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese salario con un empleado(a), por lo que en este caso se guarda -
                 * ese ID y se envia un true al método: EliminarSalarios. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * salario en la base de datos, y, en consecuencia, pueda permitir la eliminación -
                 * de ese dato respectivamente. */
                if (salarioId != null)
                {
                    resultado_salario_eliminar = salarioObtenido.Result!.Id_Empleado;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable salarioId: " + salarioId);
                 * Debug.WriteLine("Para ver la variable salario: " + salario);
                 * 
                 * Debug.WriteLine("Para ver la variable salarioObtenido: " + salarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable salarioObtenido: " + salarioObtenido);
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

                string lugarOcurrido = "VerificarSalario_ParaEliminar";

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
