using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using System.Diagnostics;

namespace MuniTurrialbaAPI.Repositories
{
    public class FAQRepository : IFAQRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:
        int resultado_pregunta_crear;
        int resultado_pregunta_actualizar;
        int resultado_pregunta_eliminar;


        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public FAQRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Estos métodos sirven para crear, actualizar o eliminar a un FAQ dentro -
         * de la base de datos. */
        public async Task<bool?> CrearFAQ(FAQCreateDto faqDto, int idUsuarioParametrizado)
        {
            /* Es para saber si ese FAQ existe en la BD. Si resulta que llega a dar nulo, -
             * quiere decir que ocurrio un error durante la verificación de ese FAQ y se -
             * enviaria un nulo para que el sistema pueda mandar el mensaje correspondiente -
             * al usuario(a). */
            bool? resultado = VerificarPregunta_ParaCrear(faqDto.Pregunta);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Para ver lo que hay en resultado_pregunta_crear:
            Debug.WriteLine("Para ver resultado_pregunta_crear: " + resultado_pregunta_crear);

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_pregunta_crear es igual a 0, quiere decir que esa pregunta -
             * no existe en la base de datos.
             * 
             * Por lo que entonces si se podria crear esa pregunta en la base de datos -
             * respectivamente. */
            if (resultado != false && resultado_pregunta_crear == 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var nuevoFAQ = await conexionBD.ExecuteAsync("PROCED_CrearFAQs",
                        //Se coloca los parametros:
                        new
                        {
                            Pregunta = faqDto.Pregunta,
                            Respuesta = faqDto.Respuesta,
                            Tipo_Prioridad = faqDto.Tipo_Prioridad,
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo crear la pregunta, y de ese -
                     * modo mandar un error respectivamente. */
                    var respuestaFAQ = nuevoFAQ.ToString();
                    if (respuestaFAQ == null)
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
                    string lugarOcurrido = "CrearFAQ";

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

        public async Task<bool?> ActualizarFAQ(FAQCreateDto faqDto, string preguntaParametrizada, int idUsuarioParametrizado)
        {
            /* Es para saber si ese FAQ existe en la BD. Si resulta que llega a dar nulo, -
             * quiere decir que ocurrio un error durante la verificación de ese FAQ y se -
             * enviaria un nulo para que el sistema pueda mandar el mensaje correspondiente -
             * al usuario(a). */
            bool? resultado = VerificarPregunta_ParaActualizar(preguntaParametrizada, idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_pregunta_actualizar es igual a 0, quiere decir que esa -
             * pregunta no existe en la base de datos.
             * 
             * Por lo que entonces si se podria crear esa pregunta en la base de -
             * datos respectivamente. */
            if (resultado != false && resultado_pregunta_actualizar != 0)
            {
                try
                {
                    /* Se guarda el ID obtenido de la pregunta al cual se quiere -
                     * actualizar. */
                    int idFAQ = resultado_pregunta_actualizar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_FAQ",
                        //Se coloca los parametros:
                        new
                        {
                            Pregunta = faqDto.Pregunta,
                            Respuesta = faqDto.Respuesta,
                            Tipo_Prioridad = faqDto.Tipo_Prioridad,
                            Id_FAQ = idFAQ
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar la pregunta, y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaFAQ = resultadoActualizacion;
                    if (respuestaFAQ == 0)
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
                    string lugarOcurrido = "ActualizarFAQ";

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

        public async Task<bool?> EliminarFAQ(string preguntaParametrizada, int idUsuarioParametrizado)
        {
            /* Es para saber si ese FAQ existe en la BD. Si resulta que llega a dar nulo, -
             * quiere decir que ocurrio un error durante la verificación de ese FAQ y se -
             * enviaria un nulo para que el sistema pueda mandar el mensaje correspondiente -
             * al usuario(a). */
            bool? resultado = VerificarPregunta_ParaEliminar(preguntaParametrizada, idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_pregunta_eliminar es diferente a 0, quiere decir que esa -
             * pregunta si existe en la base de datos.
             * 
             * Por lo que entonces si se podria eliminar esa pregunta en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_pregunta_eliminar != 0)
            {
                try
                {
                    /* Se guarda el ID obtenido de la pregunta al cual se quiere -
                     * eliminar. */
                    int idFAQ = resultado_pregunta_eliminar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync("PROCED_Eliminar_FAQ",
                        //Se coloca los parametros:
                        new
                        {
                            Id_FAQ = idFAQ
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo eliminar la pregunta, y de ese -
                     * modo mandar un error respectivamente. */
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
                    string lugarOcurrido = "EliminarFAQ";

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

        

        /* Este método sirve para obtener todas las preguntas dentro la base de datos. */
        public async Task<IEnumerable<FAQEntitie>?> ObtenerFAQs()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var FAQs = await conexionBD.QueryAsync<FAQEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarFAQs", commandType:
                    System.Data.CommandType.StoredProcedure);


                /* Se cierra la conexión por temas de buenas prácticas y se devuelve todos -
                 * los preguntas respectivamente. */
                conexionBD.Close();

                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                return FAQs;
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
                string lugarOcurrido = "ObtenerFAQs";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Estos métodos sirven para verificar si esas preguntas están o no en la base de datos.
         * Y ya con eso, dirigirlos a sus respectivos métodos, como son el de crear, actualizar o -
         * eliminar una pregunta respectivamente. */
        public bool? VerificarPregunta_ParaCrear(string preguntaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la pregunta pasada por parametro es distinto de nulo, puede pasar.
            if (preguntaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_pregunta_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var faqObtenido = conexionBD.QueryFirstOrDefaultAsync<FAQEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarFAQ",
                    new
                    {
                        Pregunta = preguntaParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado.*/
                string? faqId = faqObtenido.Result?.Id.ToString();


                /* Si el ID de la pregunta que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe esa pregunta, por lo que en este caso se guarda ese ID y se -
                 * envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe esa -
                 * pregunta en la base de datos, y, en consecuencia, no permita la creación de -
                 * ese dato respectivamente. */
                if (faqId != null)
                {
                    resultado_pregunta_crear = faqObtenido.Result!.Id;

                    conexionBD.Close();

                    return false;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver la variable faqId: " + faqId);
                 * 
                 * Debug.WriteLine("Para ver la variable faqObtenido: " + faqObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable faqObtenido: " + faqObtenido);
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

                string lugarOcurrido = "VerificarPregunta_ParaCrear";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarPregunta_ParaActualizar(string preguntaParametrizada, int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la pregunta pasada por parametro es distinto de nulo, puede pasar.
            if (preguntaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_pregunta_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var faqObtenido = conexionBD.QueryFirstOrDefaultAsync<FAQEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_VerificarFAQ",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado,
                        Pregunta = preguntaParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado.*/
                string? faqId = faqObtenido.Result?.Id.ToString();


                /* Si el ID de la pregunta que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe esa pregunta, por lo que en este caso se guarda ese ID y se -
                 * envia un true al método: ActualizarFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe esa -
                 * pregunta en la base de datos, y, en consecuencia, no permita la actualización -
                 * de ese dato respectivamente. */
                if (faqId != null)
                {
                    resultado_pregunta_actualizar = faqObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable faqId: " + faqId);
                 * 
                 * Debug.WriteLine("Para ver la variable faqObtenido: " + faqObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable faqObtenido: " + faqObtenido);
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

                string lugarOcurrido = "VerificarPregunta_ParaActualizar";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarPregunta_ParaEliminar(string preguntaParametrizada, int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la pregunta pasada por parametro es distinto de nulo, puede pasar.
            if (preguntaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_pregunta_eliminar = 0;

                //Para ejecutar el procedimiento almacenado.
                var faqObtenido = conexionBD.QueryFirstOrDefaultAsync<FAQEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_VerificarFAQ",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado,
                        Pregunta = preguntaParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? faqId = faqObtenido.Result?.Id.ToString();
                Task<FAQEntitie> faq = faqObtenido;


                /* Si el ID de la pregunta que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe esa pregunta, por lo que en este caso se guarda ese ID y se -
                 * envia un true al método: EliminarFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe esa -
                 * pregunta en la base de datos, y, en consecuencia, no permita la eliminación -
                 * de ese dato respectivamente. */
                if (faqId != null)
                {
                    resultado_pregunta_eliminar = faqObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable faqId: " + faqId);
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

                string lugarOcurrido = "VerificarPregunta_ParaEliminar";

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
