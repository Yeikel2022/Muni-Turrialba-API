using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using System.Diagnostics;

namespace MuniTurrialbaAPI.Repositories
{
    public class EmpleadoRepository : IEmpleadoRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        //Variables globales:
        int resultado_empleado;
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

        /* Este método sirve para crear un FAQ dentro de la base de datos. */
        public async Task<bool?> CrearEmpleado(EmpleadoCreateDto empleado_dto, int idUsuarioParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool? resultado = VerificarEmpleado_ParaCrear(idUsuarioParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Para ver lo que hay en resultado_empleado:
            Debug.WriteLine("Para ver resultado_empleado: " + resultado_empleado);

            /* Si en la variable: resultado, que contiene la respuesta del método: -
             * ObtenerIDUsuario_PorCedula, es diferente a falso, y también, si en la -
             * variable: resultado_cedula es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro no existe -
             * en la base de datos como tal. Entonces, si se podria registrar hacia la -
             * base de datos. 
             * 
             * Esto de igual manera aplica al correo electronico respectivamente. */
            if ((resultado != null || resultado != false) && resultado_empleado == 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var nuevoEmpleado = await conexionBD.ExecuteAsync("PROCED_CrearEmpleados",
                        //Se coloca los parametros:
                        new
                        {
                            Activo = empleado_dto.Activo,
                            Departamento = empleado_dto.Departamento,
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    var respuestaEmpleado = nuevoEmpleado.ToString();
                    if (respuestaEmpleado == null)
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


        }//Fin del método.


        /* Este método sirve para actualizar la contraseña de un usuario dentro de la base de datos. */
        public async Task<bool?> ActualizarEmpleado(EmpleadoCreateDto empleado_dto, int idUsuarioParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool? resultadoEmpleado = VerificarEmpleado_ParaActualizar(idUsuarioParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la contraseña -
             * respectivamente. */
            if (resultadoEmpleado != false && resultado_empleado_actualizar != 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync(
                        "PROCED_Actualizar_Empleado",
                        new
                        {
                            Activo = empleado_dto.Activo,
                            Departamento = empleado_dto.Departamento,
                            Id_Usuario = idUsuarioParametrizado
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


        /* Este método sirve para actualizar un FAQ dentro de la base de datos. */
        public async Task<bool?> EliminarEmpleado(int idUsuarioParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool? resultado = VerificarEmpleado_ParaEliminar(idUsuarioParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: Resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la foto -
             * de perfil respectivamente. */
            if ((resultado != null || resultado != false) && resultado_empleado_eliminar != 0)
            {
                try
                {
                    int idUsuario_Empleado = resultado_empleado_eliminar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync(
                        "PROCED_Eliminar_Empleado",
                        new
                        {
                            Id_Usuario = idUsuario_Empleado
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
        }//Fin del método.



        /* Este método sirve para obtener todos los usuarios dentro la base de datos. */
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

                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return empleados;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);
                return null;
            }//Fin del try catch.

        }//Fin del método.


        /* Este método sirve para obtener un usuario por medio del correo en la base de datos. */
        public async Task<EmpleadoEntitie?>? ObtenerEmpleado_PorIdUsuario(int idUsuarioParametrizada)
        {
            //Crea la conexión.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var empleado_Obtenido = await conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    "PROCED_Consultar_Empleado_X_Usuario",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);

                /* Para que verificar si trajo el correo respectivo. 
                 * Además de permitir nulos a traves del simbolo: ? */
                string? verDatos = empleado_Obtenido?.Id_Usuario.ToString();
                Debug.WriteLine("Datos que trajo: " + verDatos);

                /* Si el usuario que se obtuvo es nulo, quiere decir -
                 * que ese correo no existe dentro de la BD. Por lo -
                 * que se envia como respuesta un nulo. */
                if (empleado_Obtenido?.ToString() == null)
                {
                    Debug.WriteLine("Datos que trajo: " + verDatos);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    //Para cerrar la conexión, esto por temas de buenas prácticas.
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return null;
                }

                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                //Para cerrar la conexión, esto por temas de buenas prácticas.
                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return empleado_Obtenido;

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
        public bool? VerificarEmpleado_ParaCrear(int? idUsuarioParametrizado)
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
                resultado_empleado = 0;

                //Para ejecutar el procedimiento almacenado.
                var empleado_Obtenido = conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarEmpleado",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "faqId" -
                 * respectivamente. */
                string? empleadoId = empleado_Obtenido.Result?.Id.ToString();
                Debug.WriteLine("Para ver la variable empleadoId: " + empleadoId);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (empleadoId != null)
                {
                    resultado_empleado = empleado_Obtenido.Result!.Id;

                    Debug.WriteLine("Para ver la variable empleado_Obtenido: " + empleado_Obtenido);
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

                Debug.WriteLine("Para ver la variable faq_Obtenido: " + empleado_Obtenido);
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


        /* Este método sirve para obtener el ID de un usuario por medio de la cédula, esto por medio -
         * de la base de datos respectivamente. */
        public bool? VerificarEmpleado_ParaActualizar(int? idUsuarioParametrizado)
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
                resultado_empleado_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var empleado_Obtenido = conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarEmpleado",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "faqId" -
                 * respectivamente. */
                string? empleadoId = empleado_Obtenido.Result?.Id.ToString();
                Debug.WriteLine("Para ver la variable empleadoId: " + empleadoId);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (empleadoId != null)
                {
                    resultado_empleado_actualizar = empleado_Obtenido.Result!.Id_Usuario;

                    Debug.WriteLine("Para ver la variable empleado_Obtenido: " + empleado_Obtenido);
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

                Debug.WriteLine("Para ver la variable faq_Obtenido: " + empleado_Obtenido);
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


        /* Este método sirve para obtener el ID de un usuario por medio de la cédula, esto por medio -
         * de la base de datos respectivamente. */
        public bool? VerificarEmpleado_ParaEliminar(int? idUsuarioParametrizado)
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
                resultado_empleado_eliminar = 0;

                //Para ejecutar el procedimiento almacenado.
                var empleado_Obtenido = conexionBD.QueryFirstOrDefaultAsync<EmpleadoEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarEmpleado",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "faqId" -
                 * respectivamente. */
                string? empleadoId = empleado_Obtenido.Result?.Id.ToString();

                Task<EmpleadoEntitie?> empleadoID = empleado_Obtenido;

                Debug.WriteLine("Para ver la variable empleadoId: " + empleadoId);
                Debug.WriteLine("Para ver la variable empleadoId: " + empleadoID);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (empleadoId != null)
                {
                    resultado_empleado_eliminar = empleado_Obtenido.Result!.Id_Usuario;

                    Debug.WriteLine("Para ver la variable empleado_Obtenido: " + empleado_Obtenido);
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

                Debug.WriteLine("Para ver la variable faq_Obtenido: " + empleado_Obtenido);
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





        //|========================================| FIN DE LA CLASE |========================================|
    }
}
