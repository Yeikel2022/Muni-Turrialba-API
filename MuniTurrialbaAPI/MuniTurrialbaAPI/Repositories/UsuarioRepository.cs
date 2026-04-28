using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using MuniTurrialbaAPI.Repositories.CodigoFormat;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace MuniTurrialbaAPI.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly string _connectionString;

        /* Esta variable global sirve para que el método: "ObtenerIDUsuario_PorCedula", pueda -
         * indicar si la cédula enviada, existe o no en la BD. Además de servir para la segunda -
         * validación del método: CrearUsuario. */
        int resultado_cedula;
        int resultado_correo;
        int resultado_Correo;
        string CodigoRestablecimiento;
        private string correoGuardado;


        /* Asignación: [1.1]
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public UsuarioRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Este método sirve para crear un usuario dentro de la base de datos. */
        public async Task<int?> CrearUsuario(UsuarioCreateDto userdto)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool resultado = ObtenerIDUsuario_PorCedula(userdto.Cedula);
            
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool resultadoCorreo = ObtenerIDUsuario_PorCorreo(userdto.Correo_Electronico);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();


            /* Si en la variable: resultado, que contiene la respuesta del método: -
             * ObtenerIDUsuario_PorCedula, es diferente a falso, y también, si en la -
             * variable: resultado_cedula es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro no existe -
             * en la base de datos como tal. Entonces, si se podria registrar hacia la -
             * base de datos.*/
            if ((resultado != false && resultado_cedula == 0) && (resultadoCorreo != false && resultado_correo == 0))
            {
                try
                {
                    //Esto es para poder enviar el dia y la hora en que se hizo el registro.
                    DateTime Fecha_Registro = DateTime.Now;

                    //Para ejecutar el procedimiento almacenado. NOTA: Cambiar este solo por el EXECUTE.
                    var nuevoUsuario = await conexionBD.QueryFirstAsync<dynamic>(
                        //Procedimiento almacenado:
                        "PROCED_CrearUsuarios",
                        /*Se coloca los parametros*/
                        new
                        {
                            Nombre = userdto.Nombre,
                            Apellido_1 = userdto.Apellido_1,
                            Apellido_2 = userdto.Apellido_2,
                            Edad = userdto.Edad,
                            Cedula = userdto.Cedula,
                            Telefono = userdto.Telefono,
                            Correo_Electronico = userdto.Correo_Electronico,
                            Contraseña = userdto.Contraseña,
                            Fecha_Creacion = Fecha_Registro,
                            Imagen_Perfil = userdto.Imagen_Perfil,
                            Id_Rol = userdto.Id_Rol
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    //Esto es para mostrar al API el id del usuario.
                    int id_Usuario = Convert.ToInt32(nuevoUsuario.Id);

                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return id_Usuario;
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

            return 0;

        }//Fin del método.

        /* Este método sirve para crear un usuario dentro de la base de datos. */
        public async Task<string> EnviarCorreo(string correoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool resultadoCorreo = ValidarUsuario_PorCorreo(correoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();            

            /* Si en la variable: resultado, que contiene la respuesta del método: -
             * ObtenerIDUsuario_PorCedula, es diferente a falso, y también, si en la -
             * variable: resultado_cedula es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro no existe -
             * en la base de datos como tal. Entonces, si se podria registrar hacia la -
             * base de datos.*/
            if (resultadoCorreo != false && resultado_Correo != 0)
            {
                try
                {
                    CodigoRestablecimiento = null;

                    //CodigoRestablecimiento = codigoFormato.CodigoGenerado(6);
                    CodigoRestablecimiento = CodigoGenerado(6);


                    string correoOriginario = "pruebahola472@gmail.com";
                    string contraseñaOrginario = "ijqa pciw sxbm ilaq";

                    string tituloCorreo = "Código de recuperación de cuenta.";
                    string mensajeCorreo = "<div>" + "<img src=\"https://www.muniturrialba.go.cr/images/speasyimagegallery/albums/1/images/turri15.jpg\" width=\"1920\" height=\"2080\">" + "</div>" + 
                       "<h3>" + "<p text-align: justify>" + "¡Hola! <br> Gracias por mandar tu solicitud de restablecimiento de contraseña. <br>" + 
                       "</p>" + "</h3>" + "<body>" + "<p text-align: justify>" + "Aqui te proporcionamos un código de restablecimiento para que puedas ingresar a la aplicación y recuperar tu cuenta. <br>" + 
                       "</p>" + "<p text-align: justify>" + "<b>Advertencia: <u>Este código es de uso confidencial</u></b>, no compartas este código con ningún otro por ningún mótivo. <br>" + "</p>" +
                       "<p text-align: justify>" + "Aqui tienes tu código: " + CodigoRestablecimiento + "</p>" + "<p text-align: justify>" + "Muchas gracias por usar nuestros servicios. <br> MuniTurrialba.<br>" + "</p>" +
                       "<img src=\"https://www.muniturrialba.go.cr/images/speasyimagegallery/albums/1/images/turri7.jpg\" width=\"2000\" height=\"100\">" + "</body>";

                    MailMessage mailMessage = new MailMessage
                    {
                        //Este es para indicar quien manda el mensaje
                        From = new MailAddress(correoOriginario),
                        
                        //Este es para dar el titulo del correo.
                        Subject = tituloCorreo,
                        
                        //Este es para colocar todo el mensaje del correo.
                        Body = mensajeCorreo,
                        
                        //Permite poder usar HTML.
                        IsBodyHtml = true                      
                    };



                    SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
                    {
                        /* Este es para colocar las credenciales del correo al cual usara el FROM. 
                         * Además de colocar su contraseña*/
                        Credentials = new NetworkCredential(correoOriginario, contraseñaOrginario),
                        
                        //Permite usar SSL para el cibrado.
                        EnableSsl = true,
                        
                        //Esto indica como va a ser enviado el correo.
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        
                        //Esto es para evitar que use credenciales por defecto.
                        UseDefaultCredentials = false                        
                    };

                    mailMessage.To.Add(correoParametrizado);
                    client.Send(mailMessage);
                    Debug.WriteLine("¡Se envio el mensaje!");
                    client.Dispose();

                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return "1";
                }
                catch (Exception error)
                {
                    Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error);
                    return "0";
                }//Fin del try catch.

            }//Fin del IF.

            Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

            conexionBD.Close();
            Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

            return "2";

        }//Fin del método.

        
        /* Este método sirve para crear un usuario dentro de la base de datos. */
        public bool VerificarCodigo(string codigoParametrizado)
        {            
            try
            {
                if (codigoParametrizado != CodigoRestablecimiento)
                {
                    return false;
                }

                return true;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);
                return false;
            }//Fin del try catch.

        }//Fin del método.

        public async Task<bool> ActualizarContraseñaUsuario(string contraseñaParametrizado, string correoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool resultadoCorreo = ValidarUsuario_PorCorreo(correoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* S.*/
            if (resultadoCorreo != false && resultado_Correo != 0)
            {
                try
                {
                    var resultadoActualizacion = await conexionBD.ExecuteAsync(
                        "PROCED_Actualizar_Contraseña_X_IdUsuario",
                        new
                        {
                            Correo_Electronico = correoParametrizado,
                            Contraseña = contraseñaParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);

                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                    //Se cierra la conexión por temas de buenas prácticas.
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    Debug.WriteLine(resultadoActualizacion.ToString());
                    
                    return true;
                }
                catch (Exception error)
                {
                    Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error);
                    return false;
                }//Fin del try catch.

            }//Fin del IF.

            Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

            conexionBD.Close();
            Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

            return false;
        }




        /* Este método sirve para obtener todos los usuarios dentro la base de datos. */
        public async Task<IEnumerable<UsuarioEntitie>?> ObtenerUsuarios()
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var todos_Usuarios = await conexionBD.QueryAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarUsuarios", commandType:
                    System.Data.CommandType.StoredProcedure);

                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                
                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return todos_Usuarios;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);
                return null;
            }//Fin del try catch.

        }//Fin del método.


        /* Este método sirve para obtener un usuario por medio del correo en la base de datos. */
        public async Task<UsuarioEntitie?>? ObtenerUsuario_PorCorreo(string correoElectronico)
        {
            //Crea la conexión.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var usuario_Obtenido = await conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    "PROCED_Consultar_Usuario_X_Correo",
                    new
                    { 
                        Correo = correoElectronico 
                    },
                    commandType: System.Data.CommandType.StoredProcedure);

                /* Para que verificar si trajo el correo respectivo. 
                 * Además de permitir nulos a traves del simbolo: ? */
                string? verDatos = usuario_Obtenido?.Correo_Electronico;
                Debug.WriteLine("Datos que trajo: " + verDatos);

                /* Si el usuario que se obtuvo es nulo, quiere decir -
                 * que ese correo no existe dentro de la BD. Por lo -
                 * que se envia como respuesta un nulo. */
                if(usuario_Obtenido?.ToString() == null) 
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

                return usuario_Obtenido;

            } catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error);
                return null;
            }
        }


        /* Este método sirve para obtener el ID de un usuario por medio de la cédula, -
         * esto por medio de la base de datos respectivamente. */
        public bool ObtenerIDUsuario_PorCedula(string cedulaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if (cedulaParametrizada != null)
            {
                try
                {
                    //Primero hay que resetear esta variable para evitar una confusión más adelante.
                    resultado_cedula = 0;

                    //Para ejecutar el procedimiento almacenado.
                    var usuario_Obtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                        //Procedimiento almacenado:
                        "PROCED_Consultar_IdUsuario_X_Cedula",
                        new
                        {
                            Cedula = cedulaParametrizada
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Guarda el ID que trae el procedimiento almacenado. 
                     * Además de verificar si trae algo la variable: "usuario_Obtenido" -
                     * respectivamente.*/
                    string? usuarioId = usuario_Obtenido.Result?.Id.ToString();


                    /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                     * que ya existe un usuario con esa cédula, por lo que, en este caso se guarda -
                     * ese ID y se envia un falso al método: CrearUsuario. 
                     * 
                     * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                     * usuario como tal en la base de datos, y en consecuencia, este no permita -
                     * que se realice la creación de ese dato respectivamente.*/
                    if (usuarioId != null)
                    {
                        resultado_cedula = usuario_Obtenido.Result!.Id;

                        Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                        
                        conexionBD.Close();
                        Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                        return false;
                    }


                    /* Ahora, si no entra en el IF, significa que no existe un usuario con esa -
                     * cédula respectiva. Por ende, no seria necesario modificar la variable: -
                     * resultado_cedula, para que guarde ese ID, esto porque el resultado ya -
                     * esta indicando que no existe como tal (osea que es nulo).
                     * 
                     * Entonces, en este caso simplemente se mantiene asi como esta la variable -
                     * (osea cero), y se envia un true al método: CrearUsuario. De modo que en -
                     * dicho método, puedan tener conocimiento de que ese usuario no existe como -
                     * tal en la base de datos, y en consecuencia, pueda permitir la creación de -
                     * ese dato respectivamente. */
                    Debug.WriteLine("Para ver la variable resultado_cedula: " + resultado_cedula);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                    
                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return true;
                }
                catch (Exception error)
                {
                    Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error);

                    return false;
                }//Fin del try catch.

            }//Fin del IF.

            return false;

        }//Fin del método.

        public bool ObtenerIDUsuario_PorCorreo(string correoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si el correo pasado por parametro es igual a nulo, entonces no puede seguir.
            if (correoParametrizado == null)
            {
                return false;
            }


            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_correo = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuario_Obtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_IdUsuario_X_Correo",
                    new
                    {
                        Correo_Electronico = correoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "usuario_Obtenido" -
                 * respectivamente.*/
                string? usuarioId = usuario_Obtenido.Result?.Id.ToString();


                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearUsuario. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente.*/
                if (usuarioId != null)
                {
                    resultado_correo = usuario_Obtenido.Result!.Id;

                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                    return false;
                }


                /* Ahora, si no entra en el IF, significa que no existe un usuario con ese -
                 * correo electronico. Por ende, no seria necesario modificar la variable: -
                 * resultado_correo, para que guarde ese ID, esto porque el resultado ya -
                 * esta indicando que no existe como tal (osea que es nulo).
                 * 
                 * Entonces, en este caso simplemente se mantiene asi como esta la variable -
                 * (osea cero), y se envia un true al método: CrearUsuario. De modo que en -
                 * dicho método, puedan tener conocimiento de que ese usuario no existe como -
                 * tal en la base de datos, y en consecuencia, pueda permitir la creación de -
                 * ese dato respectivamente. */
                Debug.WriteLine("Para ver la variable resultado_correo: " + resultado_correo);
                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return true;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);

                return false;
            }//Fin del try catch.

        }//Fin del método.

        public bool ValidarUsuario_PorCorreo(string correoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si el correo pasado por parametro es igual a nulo, entonces no puede seguir.
            if (correoParametrizado == null)
            {
                return false;
            }


            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_Correo = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuario_Obtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_IdUsuario_X_Correo",
                    new
                    {
                        Correo_Electronico = correoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "usuario_Obtenido" -
                 * respectivamente.*/
                string? usuarioId = usuario_Obtenido.Result?.Id.ToString();


                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearUsuario. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente.*/
                if (usuarioId == null)
                {                    
                    /* Ahora, si no entra en el IF, significa que no existe un usuario con ese -
                     * correo electronico. Por ende, no seria necesario modificar la variable: -
                     * resultado_correo, para que guarde ese ID, esto porque el resultado ya -
                     * esta indicando que no existe como tal (osea que es nulo).
                     * 
                     * Entonces, en este caso simplemente se mantiene asi como esta la variable -
                     * (osea cero), y se envia un true al método: CrearUsuario. De modo que en -
                     * dicho método, puedan tener conocimiento de que ese usuario no existe como -
                     * tal en la base de datos, y en consecuencia, pueda permitir la creación de -
                     * ese dato respectivamente. */
                    Debug.WriteLine("Para ver la variable resultado_correo: " + resultado_Correo);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                    return false;
                }

                resultado_Correo = usuario_Obtenido.Result!.Id;
                Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                conexionBD.Close();
                Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

                return true;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);

                return false;
            }//Fin del try catch.

        }//Fin del método.        


        private string CodigoGenerado(int longitudParametrizada)
        {
            const string cadenaCaracteres = "WfjndFin*MBfu*HAdPNe$GTm(L5cf{Lc!94SLCZG+:7bY&U2+PnM(ER9Nurk.bZ&6xxzcw:A!*W?-xnA:]%u=yLmCSApY2K[5K##";

            StringBuilder constructorString = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < longitudParametrizada; i++)
            {
                int index = random.Next(cadenaCaracteres.Length);
                constructorString.Append(cadenaCaracteres[index]);
            }

            return constructorString.ToString();
        }


        //|========================================| FIN DE LA CLASE |========================================|
    }
}
