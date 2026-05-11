using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using System.Diagnostics;

namespace MuniTurrialbaAPI.Repositories
{
    public class PermisoRepository : IPermisoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;


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
        /*public async Task<bool?> CrearPermiso_Usuario(PermisoCreateDto permisodto)
        {
            //Es para saber si ese usuario existe en la BD.
            var resultado = ObtenerUsuario_PorId(permisodto.Id_Usuario);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();


             Si en la variable: resultado, que contiene la respuesta del método: -
             * ObtenerUsuario_PorId, es diferente a nulo, y también, si en la -
             * variable: resultado_usuario es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro no existe -
             * en la base de datos como tal. Entonces, si se podria registrar hacia la -
             * base de datos.
            if (resultado != null && resultado_usuario == 0)
            {
                try
                {
                    var resultadoPermiso = await conexionBD.ExecuteAsync(
                        "PROCED_Crear_Permisos_X_IdUsuario",
                        new
                        {
                            Leer = permisodto.Leer,
                            Crear = permisodto.Crear,
                            Actualizar = permisodto.Actualizar,
                            Eliminar = permisodto.Eliminar,
                            Id_Usuario = permisodto.Id_Usuario
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

        }//Fin del método.*/


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


        //|========================================| FIN DE LA CLASE |========================================|
    }
}
