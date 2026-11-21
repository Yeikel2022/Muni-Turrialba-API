using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public class PermisoRepository : IPermisoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:
        int resultado_permisos_usuario_actualizar;

        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public PermisoRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Este método sirve para actualizar los permisos de un usuario(a) dentro -
         * de la base de datos. */
        public async Task<bool?> ActualizarPermisos_Usuario(PermisoCreateDto permisoDto, int idUsuarioParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente al usuario(a). */
            bool? resultado = VerificarPermisosUsuario_ParaActualizar(idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_permisos_usuario_actualizar es diferente a 0, quiere decir -
             * que ese usuario(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese usuario(a) en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_permisos_usuario_actualizar != 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_ActualizarPermisos_Usuario",
                        //Se coloca los parametros:
                        new
                        {
                            Leer = permisoDto.Leer,
                            Crear = permisoDto.Crear,
                            Actualizar = permisoDto.Actualizar,
                            Eliminar = permisoDto.Eliminar,
                            Descargar = permisoDto.Descargar,
                            Compartir = permisoDto.Compartir,
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar el usuario(a), y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaPermiso = resultadoActualizacion;
                    if (respuestaPermiso == 0)
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
                    string lugarOcurrido = "ActualizarPermisos_Usuario";

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



        /* Este método sirve para obtener los permisos de un usuario(a) dentro de la base -
         * de datos. */
        public async Task<PermisoEntitie?>? ObtenerPermisos_Usuario(int idUsuarioParametrizado)
        {
            //Crea la conexión.
            using var conexionBD = CreateConnection();
            
            try
            {
                //Para ejecutar el procedimiento almacenado.
                var permisosObtenidos = await conexionBD.QueryFirstAsync<PermisoEntitie>(
                    "PROCED_Consultar_Permisos_X_IdUsuario",
                    //Se coloca los parametros:
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda los permisos del usuario(a) que trae el procedimiento almacenado. */
                PermisoEntitie? permisos = permisosObtenidos;


                /* Si el usuario(a) que se obtuvo desde la BD es igual a nulo, significa -
                 * que no existe ese usuario(a), por lo que en este caso se cierra la -
                 * conexión de la base de datos y se envia un nulo como respuesta -
                 * respectivamente. */
                if (permisos == null)
                {
                    //Para cerrar la conexión, esto por temas de buenas prácticas.
                    conexionBD.Close();

                    return null;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Datos de los permisos del usuario(a): " + permisos);
                 * Debug.WriteLine("Datos del usuario(a): " + permisosObtenidos);
                 * 
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                //Para cerrar la conexión, esto por temas de buenas prácticas.
                conexionBD.Close();

                return permisosObtenidos;
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
                string lugarOcurrido = "ObtenerPermisos_Usuario";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Este método sirve para verificar si ese usuario(a) está o no en la base de datos.
         * Y ya con eso, dirigirlo al método de actualizar los permisos respectivamente. */
        public bool? VerificarPermisosUsuario_ParaActualizar(int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_permisos_usuario_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var permisoObtenido = conexionBD.QueryFirstOrDefaultAsync<PermisoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Permisos_X_IdUsuario",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado.*/
                string? permisoId = permisoObtenido.Result?.Id_Usuario.ToString();


                /* Si el ID del usuario(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese usuario(a), por lo que en este caso se guarda ese ID y se -
                 * envia un true al método: ActualizarPermisos_Usuario. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * usuario(a) en la base de datos, y, en consecuencia, pueda permitir la -
                 * actualización de ese dato respectivamente. */
                if (permisoId != null)
                {
                    resultado_permisos_usuario_actualizar = permisoObtenido.Result!.Id_Usuario;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable permisoId: " + permisoId);
                 * 
                 * Debug.WriteLine("Para ver la variable permisoObtenido: " + permisoObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable permisoObtenido: " + permisoObtenido);
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

                string lugarOcurrido = "VerificarPermisosUsuario_ParaActualizar";

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
