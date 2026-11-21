using Dapper;
using Microsoft.Data.SqlClient;

namespace MuniTurrialbaAPI.Repositories
{
    public class HistoricoRepository : IHistoricoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public HistoricoRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Este método sirve para crear un registro de un historico dentro de la -
         * base de datos. */
        public async void CrearHistorico(string nombreUsuarioParametrizado, string correoParametrizado, string accionParametrizado, string DescripcionParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                /* Aqui lo que se hace es simplemente tomar la fecha y hora en que -
                 * se realizo una ejecución de un método respectivamente. */
                DateTime Fecha_Realizada = DateTime.Now;

                //Para ejecutar el procedimiento almacenado.
                var nuevoHistorico = await conexionBD.ExecuteAsync("PROCED_CrearRegistroHistorico",
                    //Se coloca los parametros:
                    new
                    {
                        Nombre_Usuario = nombreUsuarioParametrizado,
                        Correo_Electronico = correoParametrizado,
                        Accion = accionParametrizado,
                        Descripcion = DescripcionParametrizado,
                        Fecha_Realizada = Fecha_Realizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                 * true como respuesta.*/
                conexionBD.Close();

                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */
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
                string lugarOcurrido = "CrearHistorico";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);
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
    }
}
