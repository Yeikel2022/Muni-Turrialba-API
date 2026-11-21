using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using System.Diagnostics;

namespace MuniTurrialbaAPI.Repositories
{
    public class InicioSesionRepository : IInicioSesionRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variable global:
        int resultado_inicio_sesion_crear;


        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public InicioSesionRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);



        //            |==============| Zona de los métodos  |==============|

        /* Este método sirve para crear un registro de inicio de sesión dentro de la base de datos. */
        public async Task<bool?> CrearRegistroInicioSesion(InicioSesionCreateDto sesionDto, int idUsuarioParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarUsuario_ParaCrear(idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Para ver lo que hay en resultado_empleado:
            Debug.WriteLine("Para ver resultado_inicio_sesion_crear: " + resultado_inicio_sesion_crear);

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_inicio_sesion_crear es diferente a 0, quiere decir que ese -
             * usuario(a) existe en la base de datos.
             * 
             * Por lo que entonces si se podria crear un registro sobre los inicios -
             * de sesión en la base de datos respectivamente. */
            if (resultado != false && resultado_inicio_sesion_crear != 0)
            {
                try
                {
                    /* Aqui lo que se esta haciendo es parsear la hora a uno de -
                     * tipo: TimeSpan, esto porque si se deja la hora como estaba -
                     * (osea en TimeOnly), podria causar un error de tipo: -
                     * System.NotSupportedException, esto porque al intentar -
                     * registrarlo en la base de datos, el tipo TimeOnly no -
                     * lo soporta, de ahi el porque se parsea respectivamente. */
                    var hora_Parseado = sesionDto.Hora.ToTimeSpan();

                    //Para ejecutar el procedimiento almacenado.
                    var nuevoRegistro = await conexionBD.ExecuteAsync("PROCED_CrearRegistro_InicioSesion",
                        //Se coloca los parametros:
                        new
                        {
                            Fecha_Inicio_Sesion = sesionDto.Fecha_Inicio_Sesion,
                            Hora = hora_Parseado,
                            Ultima_Conexion = sesionDto.Ultima_Conexion,
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo crear el registro de inicio de -
                     * sesión, y de ese modo mandar un error respectivamente. */
                    var respuestaRegistro = nuevoRegistro.ToString();
                    if (respuestaRegistro == null)
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
                    string lugarOcurrido = "CrearRegistroInicioSesion";

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



        /* Este método sirve para obtener todos los inicios de sesión de los usuarios(as) -
         * dentro de la base de datos. */
        public async Task<IEnumerable<ExtensionInicioSesionEntitie>?> ObtenerRegistros_InicioSesion()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var iniciosSesion = await conexionBD.QueryAsync<ExtensionInicioSesionEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarRegistros_InicioSesion", commandType:
                    System.Data.CommandType.StoredProcedure);


                /* Se cierra la conexión por temas de buenas prácticas y se devuelve todos -
                 * los registros de inicios de sesión como respuesta.*/
                conexionBD.Close();

                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                return iniciosSesion;
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
                string lugarOcurrido = "ObtenerRegistros_InicioSesion";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Este método sirve para verificar si ese empleado(a) está o no en la base de datos.
         * Y ya con eso, dirigirlos al método de crear un registro respectivamente. */
        public bool? VerificarUsuario_ParaCrear(int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_inicio_sesion_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var sesionObtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Usuario_X_Id",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado.*/
                string? sesionId = sesionObtenido.Result?.Id.ToString();
                Task<UsuarioEntitie?> usuario = sesionObtenido;


                /* Si el ID del registro de inicio de sesión que se obtuvo desde la BD es -
                 * distinto a nulo, significa que ya existe ese registro, por lo que en -
                 * este caso se guarda ese ID y se envia un true al método: CrearRegistroInicioSesion. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * registro en la base de datos, y, en consecuencia, pueda permitir la creación -
                 * de ese dato respectivamente. */
                if (sesionId != null)
                {
                    resultado_inicio_sesion_crear = sesionObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable sesionId: " + sesionId);
                 * Debug.WriteLine("Para ver la variable usuario: " + usuario);
                 * 
                 * Debug.WriteLine("Para ver la variable sesionObtenido: " + sesionObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable sesionObtenido: " + sesionObtenido);
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

                string lugarOcurrido = "VerificarUsuario_ParaCrear";

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
