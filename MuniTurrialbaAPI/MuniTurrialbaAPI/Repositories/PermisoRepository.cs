using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using System.Diagnostics;

namespace MuniTurrialbaAPI.Repositories
{
    public class PermisoRepository : IPermisoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:;
        int resultado_permisos_usuario_crear;
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

        /* Este método sirve para crear los permisos de un usuario dentro de la base -
         * de datos. */
        public async Task<bool?> CrearPermisos_Usuario(PermisoCreateDto permisodto, int idUsuarioParametrizado)
        {
            //Es para saber si ese usuario existe en la BD.
            var resultado = VerificarPermisosUsuario_ParaCrear(idUsuarioParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();


            /* Si en la variable: resultado, que contiene la respuesta del método: -
             * ObtenerUsuario_PorId, es diferente a nulo, y también, si en la -
             * variable: resultado_permisos_usuario_crear es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro no existe -
             * en la base de datos como tal. Entonces, si se podria registrar hacia la -
             * base de datos. */
            if ((resultado != null || resultado != false) && resultado_permisos_usuario_crear == 0)
            {
                try
                {
                    int idUsuario_Permiso = resultado_permisos_usuario_crear;

                    var resultadoPermiso = await conexionBD.ExecuteAsync(
                        "PROCED_CrearPermisos",
                        new
                        {
                            Leer = permisodto.Leer,
                            Crear = permisodto.Crear,
                            Actualizar = permisodto.Actualizar,
                            Eliminar = permisodto.Eliminar,
                            Id_Usuario = idUsuario_Permiso
                        },
                        commandType: System.Data.CommandType.StoredProcedure);

                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return true;
                }
                catch (Exception error)
                {
                    Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error);
                    return null;
                }//Fin del try catch.

            }//Fin del IF.

            Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

            conexionBD.Close();
            Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

            return false;

        }//Fin del método.


        /* Este método sirve para actualizar la contraseña de un usuario dentro de la base de datos. */
        public async Task<bool?> ActualizarPermisos_Usuario(PermisoCreateDto permisodto, int idUsuarioParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool? resultadoPermisos = VerificarPermisosUsuario_ParaActualizar(idUsuarioParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la contraseña -
             * respectivamente. */
            if (resultadoPermisos != false && resultado_permisos_usuario_actualizar != 0)
            {
                try
                {
                    int idUsuario = resultado_permisos_usuario_actualizar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync(
                        "PROCED_ActualizarPermisos_Usuario",
                        new
                        {
                            Leer = permisodto.Leer,
                            Crear = permisodto.Crear,
                            Actualizar = permisodto.Actualizar,
                            Eliminar = permisodto.Eliminar,
                            Id_Usuario = idUsuario
                        },
                        commandType: System.Data.CommandType.StoredProcedure);

                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    //Para ver si se pudo actualizar:
                    Debug.WriteLine(resultadoActualizacion.ToString());

                    return true;
                }
                catch (Exception error)
                {
                    Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error);
                    return null;
                }//Fin del try catch.

            }//Fin del IF.

            Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

            conexionBD.Close();
            Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

            return false;
        }//Fin del método.





        /* Este método sirve para obtener los permisos de un usuario dentro de la base -
         * de datos. */
        public async Task<PermisoEntitie?>? ObtenerPermisosUsuario(int idUsuarioParametrizado)
        {
            //Crea la conexión.
            using var conexionBD = CreateConnection();
            
            try
            {
                //Para ejecutar el procedimiento almacenado.
                var permisos_UsuarioObtenido = await conexionBD.QueryFirstAsync<PermisoEntitie>(
                    "PROCED_Consultar_Permisos_X_IdUsuario",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);

                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return permisos_UsuarioObtenido;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);

                return null;
            }//Fin del try catch.

        }//Fin del método.


        /* Este método sirve para obtener los permisos de un usuario dentro de la base -
         * de datos. */
        public bool? VerificarPermisosUsuario_ParaCrear(int? idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if (idUsuarioParametrizado == null)
            {
                return false;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_permisos_usuario_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var permiso_Obtenido = conexionBD.QueryFirstOrDefaultAsync<PermisoEntitie>(
                    "PROCED_Consultar_Permisos_X_IdUsuario",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "permisoId" -
                 * respectivamente. */
                string? permisoId = permiso_Obtenido.Result?.Id.ToString();

                Task<PermisoEntitie?> permisoID = permiso_Obtenido;

                Debug.WriteLine("Para ver la variable permisoId: " + permisoId);
                Debug.WriteLine("Para ver la variable permisoId: " + permisoID);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (permisoId != null)
                {
                    resultado_permisos_usuario_crear = permiso_Obtenido.Result!.Id;

                    Debug.WriteLine("Para ver la variable permiso_Obtenido: " + permiso_Obtenido);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return true;
                }


                /* Ahora, si no entra en el IF, significa que no existe un usuario con ese -
                 * correo electronico. Por ende, no seria necesario modificar la variable: -
                 * resultado_correo para que guarde ese ID, esto porque el resultado ya esta -
                 * indicando que no existe como tal (osea que es nulo).
                 * 
                 * Entonces, en este caso simplemente se mantiene asi como esta la variable -
                 * (osea cero), y se envia un true al método: CrearUsuario. De modo que en -
                 * dicho método, puedan tener conocimiento de que ese usuario no existe como -
                 * tal en la base de datos, y en consecuencia, pueda permitir la creación de -
                 * ese dato respectivamente. */

                Debug.WriteLine("Para ver la variable faq_Obtenido: " + permiso_Obtenido);
                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);


                return false;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);

                return null;
            }//Fin del try catch.

        }//Fin del método.


        /* Este método sirve para obtener los permisos de un usuario dentro de la base -
         * de datos. */
        public bool? VerificarPermisosUsuario_ParaActualizar(int? idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si el correo pasado por parametro es igual a nulo, entonces no puede seguir.
            if (idUsuarioParametrizado == null)
            {
                return false;
            }


            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_permisos_usuario_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var permiso_Obtenido = conexionBD.QueryFirstOrDefaultAsync<PermisoEntitie>(
                    "PROCED_Consultar_Permisos_X_IdUsuario",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "usuario_Obtenido" -
                 * respectivamente.*/
                string? permisoId = permiso_Obtenido.Result?.Id_Usuario.ToString();


                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso a los métodos: EnviarCorreo, ActualizarContraseñaUsuario -
                 * y CrearCodigoQR. 
                 * 
                 * De modo que en dichos métodos, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, estos no permitan -
                 * que se realicen las acciones correspondientes. */
                if (permisoId == null)
                {
                    Debug.WriteLine("Para ver la variable resultado_permisos_usuario_actualizar: " + resultado_permisos_usuario_actualizar);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                    return false;
                }


                /* Si no entra quiere decir que no existe, por lo que lo guarda para que los -
                 * métodos que fueron mencionados anteriormente puedan saber sobre dicho aspecto. */
                resultado_permisos_usuario_actualizar = permiso_Obtenido.Result.Id_Usuario!;
                Debug.WriteLine("Para ver la variable resultado_permisos_usuario_actualizar: " + resultado_permisos_usuario_actualizar);

                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return true;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);

                return null;
            }//Fin del try catch.

        }//Fin del método.   





        //|========================================| FIN DE LA CLASE |========================================|
    }
}
