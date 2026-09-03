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

        /* Este método sirve para crear un FAQ dentro de la base de datos.  */
        public async Task<bool?> CrearRegistroInicioSesion(InicioSesionCreateDto sesiondto, int idUsuarioParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool? resultado = VerificarUsuario_ParaCrear(idUsuarioParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Para ver lo que hay en resultado_empleado:
            Debug.WriteLine("Para ver resultado_inicio_sesion_crear: " + resultado_inicio_sesion_crear);

            /* Si en la variable: resultado, que contiene la respuesta del método: -
             * ObtenerIDUsuario_PorCedula, es diferente a falso, y también, si en la -
             * variable: resultado_cedula es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro no existe -
             * en la base de datos como tal. Entonces, si se podria registrar hacia la -
             * base de datos. 
             * 
             * Esto de igual manera aplica al correo electronico respectivamente.  */
            if ((resultado != null || resultado != false) && resultado_inicio_sesion_crear != 0)
            {
                try
                {
                    //DateOnly fechaInicio_Sesion_Parseado = DateOnly.Parse(sesiondto.Fecha_Inicio_Sesion);
                    var hora_Parseado = sesiondto.Hora.ToTimeSpan();
                    //DateTime ultima_Conexion_Parseado = DateTime.Parse(sesiondto.Ultima_Conexion);

                    //Para ejecutar el procedimiento almacenado.
                    var nuevoRegistro = await conexionBD.ExecuteAsync("PROCED_CrearRegistro_InicioSesion",
                        //Se coloca los parametros:
                        new
                        {
                            Fecha_Inicio_Sesion = sesiondto.Fecha_Inicio_Sesion,
                            Hora = hora_Parseado,
                            Ultima_Conexion = sesiondto.Ultima_Conexion,
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    var respuestaRegistro = nuevoRegistro.ToString();
                    if (respuestaRegistro == null)
                    {
                        return false;
                    }


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
            }

            Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

            //Se cierra la conexión por temas de buenas prácticas.
            conexionBD.Close();
            Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

            return false;


        }//Fin del método.*/



        /* Este método sirve para obtener todos los usuarios dentro la base de datos. */
        public async Task<IEnumerable<ExtensionInicioSesionEntitie>?> ObtenerRegistros_InicioSesion()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {

                //Para ejecutar el procedimiento almacenado.
                var salarios = await conexionBD.QueryAsync<ExtensionInicioSesionEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarRegistros_InicioSesion", commandType:
                    System.Data.CommandType.StoredProcedure);

                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return salarios;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);
                return null;
            }//Fin del try catch.

        }//Fin del método.


        /* Este método sirve para obtener el ID de un usuario por medio de la cédula, esto por medio -
         * de la base de datos respectivamente. */
        public bool? VerificarUsuario_ParaCrear(int? idUsuarioParametrizado)
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
                resultado_inicio_sesion_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var sesion_Obtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Usuario_X_Id",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "faqId" -
                 * respectivamente.  */
                string? inicioSesion_Id = sesion_Obtenido.Result?.Id.ToString();
                Debug.WriteLine("Para ver la variable inicioSesion_Id: " + inicioSesion_Id);

                Task<UsuarioEntitie?> usuarioID = sesion_Obtenido;

                Debug.WriteLine("Para ver la variable usuarioId: " + inicioSesion_Id);
                Debug.WriteLine("Para ver la variable usuarioId: " + usuarioID);

                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (inicioSesion_Id != null)
                {
                    resultado_inicio_sesion_crear = sesion_Obtenido.Result!.Id;

                    Debug.WriteLine("Para ver la variable sesion_Obtenido: " + sesion_Obtenido);
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

                Debug.WriteLine("Para ver la variable sesion_Obtenido: " + sesion_Obtenido);
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

        }//Fin del método.*/


        //|========================================| FIN DE LA CLASE |========================================|
    }
}
