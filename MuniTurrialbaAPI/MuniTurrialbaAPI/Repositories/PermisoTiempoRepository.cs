using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MuniTurrialbaAPI.Repositories
{
    public class PermisoTiempoRepository : IPermisoTiempoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:
        int resultado_permiso_tiempo_crear;
        int resultado_permiso_tiempo_actualizar;
        int resultado_permiso_tiempo_eliminar;


        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public PermisoTiempoRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);



        //            |==============| Zona de los métodos  |==============|

        /* Este método sirve para crear un FAQ dentro de la base de datos.  */
        public async Task<bool?> CrearPermisosTiempo(PermisoTiempoCreateDto tiempodto, int idEmpleadoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool? resultado = VerificarPermisosTiempo_ParaCrear(idEmpleadoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Para ver lo que hay en resultado_empleado:
            Debug.WriteLine("Para ver resultado_permiso_tiempo: " + resultado_permiso_tiempo_crear);

            /* Si en la variable: resultado, que contiene la respuesta del método: -
             * ObtenerIDUsuario_PorCedula, es diferente a falso, y también, si en la -
             * variable: resultado_cedula es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro no existe -
             * en la base de datos como tal. Entonces, si se podria registrar hacia la -
             * base de datos. 
             * 
             * Esto de igual manera aplica al correo electronico respectivamente.  */
            if ((resultado != null || resultado != false) && resultado_permiso_tiempo_crear == 0)
            {
                try
                {
                    DateTime fechaAsignacion_Parseado = DateTime.Parse(tiempodto.Fecha_Asignacion);
                    DateTime fechaFinalizacion_Parseado = DateTime.Parse(tiempodto.Fecha_Finalizacion);

                    //Para ejecutar el procedimiento almacenado.
                    var nuevoPermisoTiempo = await conexionBD.ExecuteAsync("PROCED_CrearPermisosTiempo",
                        //Se coloca los parametros:
                        new
                        {
                            Tipo_Permiso = tiempodto.Tipo_Permiso,
                            Descripcion = tiempodto.Descripcion,
                            Fecha_Asignacion = fechaAsignacion_Parseado,
                            Fecha_Finalizacion = fechaFinalizacion_Parseado,
                            Id_Empleado = idEmpleadoParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    var respuestaPermisoTiempo = nuevoPermisoTiempo.ToString();
                    if (respuestaPermisoTiempo == null)
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


        /* Este método sirve para actualizar la contraseña de un usuario dentro de la base de datos. */
        public async Task<bool?> ActualizarPermisosTiempo(PermisoTiempoCreateDto tiempodto, int idEmpleadoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool? resultado = VerificarPermisosTiempo_ParaActualizar(idEmpleadoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la contraseña -
             * respectivamente. */
            if ((resultado != null || resultado != false) && resultado_permiso_tiempo_actualizar != 0)
            {
                try
                {
                    DateTime fechaAsignacion_Parseado = DateTime.Parse(tiempodto.Fecha_Asignacion);
                    DateTime fechaFinalizacion_Parseado = DateTime.Parse(tiempodto.Fecha_Finalizacion);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync(
                        "PROCED_Actualizar_PermisosTiempo",
                        new
                        {
                            Tipo_Permiso = tiempodto.Tipo_Permiso,
                            Descripcion = tiempodto.Descripcion,
                            Fecha_Asignacion = fechaAsignacion_Parseado,
                            Fecha_Finalizacion = fechaFinalizacion_Parseado,
                            Id_Empleado = idEmpleadoParametrizado
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
        }//Fin del método.*/


        /* Este método sirve para actualizar un FAQ dentro de la base de datos. */
        public async Task<bool?> EliminarPermisosTiempo(int idEmpleadoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool? resultado = VerificarPermisosTiempo_ParaEliminar(idEmpleadoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: Resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la foto -
             * de perfil respectivamente. */
            if ((resultado != null || resultado != false) && resultado_permiso_tiempo_eliminar != 0)
            {
                try
                {
                    int idEmpleado = resultado_permiso_tiempo_eliminar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync(
                        "PROCED_Eliminar_PermisosTiempo",
                        new
                        {
                            Id_Empleado = idEmpleado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);

                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    //Para ver si se pudo actualizar:
                    Debug.WriteLine(resultadoEliminacion.ToString());

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



        /* Este método sirve para obtener todos los usuarios dentro la base de datos. */
        public async Task<IEnumerable<ExtensionPermisoTiempoEntitie>?> ObtenerPermisosTiempo()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {

                //Para ejecutar el procedimiento almacenado.
                var permisosTiempo = await conexionBD.QueryAsync<ExtensionPermisoTiempoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarPermisosTiempo", commandType:
                    System.Data.CommandType.StoredProcedure);

                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return permisosTiempo;
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
        public bool? VerificarPermisosTiempo_ParaCrear(int? idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if (idEmpleadoParametrizado == null)
            {
                return false;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_permiso_tiempo_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var permiso_Tiempo_Obtenido = conexionBD.QueryFirstOrDefaultAsync<PermisoTiempoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_PermisoTiempo_X_IdEmpleado",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "faqId" -
                 * respectivamente.  */
                string? permisoTiempo_Id = permiso_Tiempo_Obtenido.Result?.Id.ToString();
                Debug.WriteLine("Para ver la variable empleadoId: " + permisoTiempo_Id);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (permisoTiempo_Id != null)
                {
                    resultado_permiso_tiempo_crear = permiso_Tiempo_Obtenido.Result!.Id;

                    Debug.WriteLine("Para ver la variable permiso_Tiempo_Obtenido: " + permiso_Tiempo_Obtenido);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return false;
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

                Debug.WriteLine("Para ver la variable permiso_Tiempo_Obtenido: " + permiso_Tiempo_Obtenido);
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

        }//Fin del método.*/


        /* Este método sirve para obtener el ID de un usuario por medio de la cédula, esto por medio -
         * de la base de datos respectivamente. */
        public bool? VerificarPermisosTiempo_ParaActualizar(int? idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if (idEmpleadoParametrizado == null)
            {
                return false;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_permiso_tiempo_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var permisoTiempo_Obtenido = conexionBD.QueryFirstOrDefaultAsync<PermisoTiempoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_PermisoTiempo_X_IdEmpleado",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "faqId" -
                 * respectivamente. */
                string? permisoTiempo_Id = permisoTiempo_Obtenido.Result?.Id.ToString();
                Debug.WriteLine("Para ver la variable permisoTiempo_Id: " + permisoTiempo_Id);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (permisoTiempo_Id != null)
                {
                    resultado_permiso_tiempo_actualizar = permisoTiempo_Obtenido.Result!.Id_Empleado;

                    Debug.WriteLine("Para ver la variable permisoTiempo_Obtenido: " + permisoTiempo_Obtenido);
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

                Debug.WriteLine("Para ver la variable faq_Obtenido: " + permisoTiempo_Obtenido);
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


        /* Este método sirve para obtener el ID de un usuario por medio de la cédula, esto por medio -
         * de la base de datos respectivamente. */
        public bool? VerificarPermisosTiempo_ParaEliminar(int? idEmpleadoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if (idEmpleadoParametrizado == null)
            {
                return false;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_permiso_tiempo_eliminar = 0;


                //Para ejecutar el procedimiento almacenado.
                var permisoTiempo_Obtenido = conexionBD.QueryFirstOrDefaultAsync<PermisoTiempoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_PermisoTiempo_X_IdEmpleado",
                    new
                    {
                        Id_Empleado = idEmpleadoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "faqId" -
                 * respectivamente. */
                string? permisoTiempo_Id = permisoTiempo_Obtenido.Result?.Id.ToString();

                Task<PermisoTiempoEntitie?> permisoTiempoID = permisoTiempo_Obtenido;

                Debug.WriteLine("Para ver la variable permisoTiempoID: " + permisoTiempoID);
                Debug.WriteLine("Para ver la variable permisoTiempoID: " + permisoTiempoID);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (permisoTiempo_Id != null)
                {
                    resultado_permiso_tiempo_eliminar = permisoTiempo_Obtenido.Result!.Id_Empleado;

                    Debug.WriteLine("Para ver la variable permisoTiempo_Obtenido: " + permisoTiempo_Obtenido);
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

                Debug.WriteLine("Para ver la variable faq_Obtenido: " + permisoTiempo_Obtenido);
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
