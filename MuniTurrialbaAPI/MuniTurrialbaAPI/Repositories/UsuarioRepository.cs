using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using QRCoder;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;


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
        int resultado_cedula_actualizar;
        
        int resultado_correo;
        int resultado_Correo;
        int Resultado_Correo;
        int Resultado_Correo_QR;
        
        int resultado_usuario_eliminar;
        string CodigoRestablecimiento;


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
        public async Task<bool?> CrearUsuario(UsuarioCreateDto userdto, bool tipo)
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
             * base de datos. 
             * 
             * Esto de igual manera aplica al correo electronico respectivamente. */
            if ((resultado != false && resultado_cedula == 0) && (resultadoCorreo != false && resultado_correo == 0))
            {
                try
                {
                    //Esto es para poder enviar el dia y la hora en que se hizo el registro.
                    DateTime Fecha_Registro = DateTime.Now;
                    string contraseñaEncriptada = EncriptarContraseña(userdto.Contraseña);
                    
                    var respuestaUsuario = "";
                    
                    if (tipo == true)
                    {
                        string Nombre_Departamento = "Recursos Humanos";
                        //Para ejecutar el procedimiento almacenado.
                        var nuevoUsuario = await conexionBD.ExecuteAsync("PROCED_CrearUsuarios",
                            //Se coloca los parametros:
                            new
                            {
                                Nombre = userdto.Nombre,
                                Apellido_1 = userdto.Apellido_1,
                                Apellido_2 = userdto.Apellido_2,
                                Edad = userdto.Edad,
                                Cedula = userdto.Cedula,
                                Telefono = userdto.Telefono,
                                Correo_Electronico = userdto.Correo_Electronico,
                                Contraseña = contraseñaEncriptada,
                                Fecha_Creacion = Fecha_Registro,
                                Imagen_Perfil = userdto.Imagen_Perfil,
                                Id_Rol = userdto.Id_Rol,
                                Leer = true,
                                Crear = true,
                                Actualizar = true,
                                Eliminar = true,
                                Activo = true,
                                Departamento = Nombre_Departamento
                            },
                            commandType: System.Data.CommandType.StoredProcedure);

                        //Esto es para mostrar al API el id del usuario.
                        respuestaUsuario = nuevoUsuario.ToString();
                    }
                    else
                    {
                        //Para ejecutar el procedimiento almacenado.
                        var nuevoUsuario = await conexionBD.ExecuteAsync("PROCED_CrearUsuario_Empleado",
                            //Se coloca los parametros:
                            new
                            {
                                Nombre = userdto.Nombre,
                                Apellido_1 = userdto.Apellido_1,
                                Apellido_2 = userdto.Apellido_2,
                                Edad = userdto.Edad,
                                Cedula = userdto.Cedula,
                                Telefono = userdto.Telefono,
                                Correo_Electronico = userdto.Correo_Electronico,
                                Contraseña = contraseñaEncriptada,
                                Fecha_Creacion = Fecha_Registro,
                                Imagen_Perfil = userdto.Imagen_Perfil,
                                Id_Rol = userdto.Id_Rol,
                                Leer = true,
                                Crear = true,
                                Actualizar = true,
                                Eliminar = true
                            },
                            commandType: System.Data.CommandType.StoredProcedure);

                        //Esto es para mostrar al API el id del usuario.
                        respuestaUsuario = nuevoUsuario.ToString();
                    }



                    /* Si lo que dio el respuestaUsuario es nulo, entonces quiere decir -
                     * que no se pudo crear el usuario. */
                    if (respuestaUsuario == null || respuestaUsuario.IsNullOrEmpty())
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

            }//Fin del IF.

            Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

            conexionBD.Close();
            Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

            return false;

        }//Fin del método.


        /* Este método sirve para crear un codigo QR de un usuario. */
        public string? CrearCodigoQR(string nombreParametrizado, string apellidosParametrizados, string correoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio del correo.
            bool resultadoCorreo = ValidarUsuario_PorCorreo(correoParametrizado);

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: Resultado_Correo_QR es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria crear el código QR -
             * respectivamente. */
            if (resultadoCorreo != false && Resultado_Correo_QR != 0)
            {
                try
                {
                    /* Es un generador que nos permitira hacer el código -
                     * QR del usuario. En otras palabras, es como si se -
                     * estuviera usando un constructor básicamente. */
                    QRCodeGenerator generadorQR = new QRCodeGenerator();

                    /* Aqui lo que se esta haciendo es crear el código QR con los datos -
                     * que vamos a proporcionar en dicho código. Osea, basicamente seria -
                     * su estructura. De ahi que se esta usando los comandos QRCodeData y -
                     * el CreateQRCode que tiene el generadorQR respectivamente. 
                     *
                     * Además, también con ayuda del comando: "QRCodeGenerator.ECCLevel.H", -
                     * se estaria colocando un nivel H en dicho QR (siendo este el nivel más -
                     * alto), esto lo que significa es que ese nivel H, nos daria un 30% más -
                     * de probabilidad de recuperar los datos que están en ese código QR. -
                     * Esto en dado caso que dicho código se llegue a dañar o incluso que -
                     * se llegue a corromper respectivamente, de ahi el porque se coloca -
                     * ese nivel. */
                    QRCodeData datosCodificar = generadorQR.CreateQrCode(
                        "Nombre: " + nombreParametrizado +
                        "\nApellidos: " + apellidosParametrizados +
                        "\nCorreo: " + correoParametrizado, QRCodeGenerator.ECCLevel.H);

                    /* Luego, aqui lo que se esta haciendo es una instancia de tipo: "PngByteQRCode" -
                     * donde se esta pasando los datos que se definieron para el código QR, para así -
                     * tener, un arreglo (o lista) de bytes en un formato PNG. */
                    PngByteQRCode codigoQR = new PngByteQRCode(datosCodificar);

                    /* Y por ultimo, lo unico que faltaria es tomar la variable: "codigoQR", que contiene -
                     * el PngByteQRCode, y usar el método: GetGraphic(20), para poder crear finalmente el -
                     * código QR como una imagen de formato PNG. Y después se codifica dicho codigo QR, en -
                     * un formato de 64 caracteres, y se devuelve al usuario respectivamente.
                     * 
                     * Eso si, se coloco un 20 en el método: GetGraphic(), porque representa la cantidad de -
                     * pixeles que se quiere dibujar para cada modulo en blanco y negro, osea, basicamente -
                     * serian los pedacitos que conforman el QR en blanco y negro. */
                    byte[] codigoListo = codigoQR.GetGraphic(20);
                    string modeloQR = Convert.ToBase64String(codigoListo);

                    return modeloQR;
                }
                catch (Exception error)
                {
                    Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error);
                    return null;
                }//Fin del try catch.

            }//Fin del IF.

            return "false";
        }//Fin del método.


        /* Este método sirve para enviar un correo electronico para recuperar la cuenta. */
        public async Task<bool?> EnviarCorreo(string correoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool resultadoCorreo = ValidarUsuario_PorCorreo(correoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: resultado_Correo es igual a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria enviar el correo con -
             * el código de recuperación. */
            if (resultadoCorreo != false && resultado_Correo != 0)
            {
                try
                {
                    /* Se resetea la variable para evitar problema. 
                     * Esto por temas de buenas prácticas. */
                    CodigoRestablecimiento = null;

                    /* Se genera un código de recuperación aleatorio, -
                     * esto con una longitud de 6 digitos. */
                    CodigoRestablecimiento = CodigoGenerado(6);

                    /* Se coloca el correo originario (o transmisor) para -
                     * poder enviar el mensaje con el código de restablecimiento. */
                    string correoOriginario = "pruebahola472@gmail.com";
                    string contraseñaOrginario = "ijqa pciw sxbm ilaq";

                    string tituloCorreo = "Código de recuperación de cuenta.";
                    string mensajeCorreo = "<div>" + "<img src=\"https://www.muniturrialba.go.cr/images/speasyimagegallery/albums/1/images/turri15.jpg\" width=\"1920\" height=\"2080\">" + "</div>" + 
                       "<h3>" + "<p text-align: justify>" + "¡Hola! <br> Gracias por mandar tu solicitud de restablecimiento de contraseña. <br>" + 
                       "</p>" + "</h3>" + "<body>" + "<p text-align: justify>" + "Aqui te proporcionamos un código de restablecimiento para que puedas ingresar a la aplicación y recuperar tu cuenta. <br>" + 
                       "</p>" + "<p text-align: justify>" + "<b>Advertencia: <u>Este código es de uso confidencial</u></b>, no compartas este código con ningún otro por ningún mótivo. <br>" + "</p>" +
                       "<p text-align: justify>" + "Aqui tienes tu código: " + CodigoRestablecimiento + "</p>" + "<p text-align: justify>" + "Muchas gracias por usar nuestros servicios. <br> MuniTurrialba.<br>" + "</p>" +
                       "<img src=\"https://www.muniturrialba.go.cr/images/speasyimagegallery/albums/1/images/turri7.jpg\" width=\"2000\" height=\"100\">" + "</body>";


                    /* Aqui lo que se hace es crear el mensaje para el correo -
                     * electrónico. Básicamente es lo que uno hace cuando quiere -
                     * mandar un nuevo correo en Gmail o en Outlook. */
                    MailMessage mailMessage = new MailMessage
                    {
                        //Este es para indicar quien manda el mensaje
                        From = new MailAddress(correoOriginario),
                        
                        //Este es para dar el titulo del correo.
                        Subject = tituloCorreo,
                        
                        //Este es para colocar todo el mensaje del correo.
                        Body = mensajeCorreo,
                        
                        /* Permite poder usar HTML, ya que el mensaje del -
                         * correo electrónico lo contiene. */
                        IsBodyHtml = true                      
                    };


                    /* Aqui lo que se hace es crear la conexión para enviar -
                     * dicho correo respectivamente. 
                     * 
                     * NOTA: Se usa el smtp.gmail.com y el puerto: 587 porque -
                     * se va a utilizar el servicio de Google Mail (Gmail), si -
                     * se quiere usar otro servicio como el de Microsoft Outlook -
                     * por ejemplo, entonces se tendria que colocar su puerto y -
                     * smtp respectivo. */
                    SmtpClient clienteSmtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        /* Este es para colocar las credenciales del correo al -
                         * cual usara el FROM. Además de colocar su contraseña. */
                        Credentials = new NetworkCredential(correoOriginario, contraseñaOrginario),
                        
                        /* Permite usar SSL para el cifrado. Esto para tener -
                         * más seguridad cuando se envie el correo electrónico. */
                        EnableSsl = true,
                        
                        //Esto indica como va a ser enviado el correo.
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        
                        //Esto es para evitar que use credenciales por defecto.
                        UseDefaultCredentials = false                        
                    };


                    /* Aqui lo que se hace es agregar el correo que el usuario -
                     * habia colocado en el parametro, para que así finalmente -
                     * se envie dicho mensaje y pueda recibir el código -
                     * respectivamente. 
                     * 
                     * Ya después de haberse enviado el correo, entonces se limpia -
                     * la configuración, esto para evitar problemas y porque también -
                     * es una buena práctica al hacerlo. */
                    mailMessage.To.Add(correoParametrizado);
                    clienteSmtp.Send(mailMessage);
                    Debug.WriteLine("¡Se envio el mensaje!");
                    clienteSmtp.Dispose();

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

        
        /* Este método sirve para verficar si el código que se esta pasando por el parametro es el -
         * mismo que se envio por el correo. */
        public bool VerificarCodigo(string codigoParametrizado)
        {            
            try
            {
                /* Aqui lo que se hace es validar si el código que -
                 * se paso por el parametro es el mismo que el que -
                 * se envio por el correo electrónico. */
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


        /* Este método sirve para verificar el usuario por medio del correo y la contraseña que se -
         * esta pasando por parametro. */
        public Task<UsuarioEntitie?>? VerificarUsuario(string correoParametrizado, string contraseñaParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            var resultadoUsuario = ObtenerUsuario_PorCorreo(correoParametrizado);

            /* Aqui lo que se hace es validar si ese usuario existe. Si en dado -
             * caso da un nulo, entonces quiere decir que no existe y devolvera -
             * un nulo respectivamente. */
            if (resultadoUsuario?.Result?.ToString() == null)
            {
                return null;
            }


            try
            {
                //Se obtiene el correo y la contraseña de ese usuario:
                string correoUsuarioBD = resultadoUsuario.Result.Correo_Electronico;
                string contraseñaUsuarioBD = DesencriptarContraseña(resultadoUsuario.Result.Contraseña);
                //string contraseñaUsuarioBD = resultadoUsuario.Result.Contraseña;


                /* Aqui lo que se hace es validar si el correo y la contraseña que se -
                 * pasaron por parametro son los mismos que estan en la base de datos. 
                 * 
                 * Si es así entonces devolvera al usuario, y en caso contrario devolveria -
                 * un nulo respectivamente. */
                if (correoUsuarioBD == correoParametrizado && contraseñaUsuarioBD == contraseñaParametrizado)
                {
                    return resultadoUsuario;
                }

                return null;
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);
                return null;
            }//Fin del try catch.

        }//Fin del método.


        /* Este método sirve para actualizar la contraseña de un usuario dentro de la base de datos. */
        public async Task<bool> ActualizarContraseñaUsuario(string contraseñaParametrizado, string correoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool resultadoCorreo = ValidarUsuario_PorCorreo(correoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la contraseña -
             * respectivamente. */
            if (resultadoCorreo != false && resultado_Correo != 0)
            {
                try
                {
                    string nuevaContraseñaEncriptada = EncriptarContraseña(contraseñaParametrizado);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync(
                        "PROCED_Actualizar_Contraseña_Usuario",
                        new
                        {
                            Correo_Electronico = correoParametrizado,
                            Contraseña = nuevaContraseñaEncriptada
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
                    return false;
                }//Fin del try catch.

            }//Fin del IF.

            Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

            conexionBD.Close();
            Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);

            return false;
        }//Fin del método.


        /* Este método sirve para actualizar la foto de perfil de un usuario dentro de la base de datos. */
        public async Task<bool?> ActualizarFotoPerfil(string fotoParametrizada, string correoParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool resultadoCorreo = ValidarUsuario_PorCorreo(correoParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: Resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la foto -
             * de perfil respectivamente. */
            if (resultadoCorreo != false && Resultado_Correo != 0)
            {
                try
                {
                    /* Aqui lo que se hace es decodificar la foto que se paso por parametro, -
                     * a un arreglo de bytes. Esto porque en la BD se maneja un VARBINARY, -
                     * entonces ocupa si o si, ese arreglo de bytes. 
                     *
                     * Además, que en la documentación de Microsoft, mencionan que con -
                     * este comando: "Convert.FromBase64String(fotoParametrizada)", nos -
                     * devolvera un arreglo (o una lista) de bytes, de ahí el porque se -
                     * dice que se esta decodificando la foto respectivamente. */
                    byte[] imagen = Convert.FromBase64String(fotoParametrizada);
                    Debug.WriteLine("Para ver la variable imagen: " + imagen);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync(
                        "PROCED_Actualizar_ImagenPerfil_Usuario",
                        new
                        {
                            Correo_Electronico = correoParametrizado,
                            Imagen_Perfil = imagen
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


        /* Este método sirve para actualizar la contraseña de un usuario dentro de la base de datos. */
        public async Task<bool?> ActualizarUsuario(UsuarioCreateDto userdto, string cedulaParametrizada)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la correo.
            bool? resultadoUsuario = VerificarUsuario_ParaActualizar(cedulaParametrizada);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la contraseña -
             * respectivamente. */
            if (resultadoUsuario != false && resultado_cedula_actualizar != 0)
            {
                try
                {
                    int idUsuario = resultado_cedula_actualizar;
                    string nuevaContraseñaEncriptada = EncriptarContraseña(userdto.Contraseña);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync(
                        "PROCED_Actualizar_Usuario",
                        new
                        {
                            Nombre = userdto.Nombre,
                            Apellido_1 = userdto.Apellido_1,
                            Apellido_2 = userdto.Apellido_2,
                            Edad = userdto.Edad,
                            Cedula = userdto.Cedula,
                            Contraseña = nuevaContraseñaEncriptada,
                            Telefono = userdto.Telefono,
                            Correo_Electronico = userdto.Correo_Electronico,
                            Id_Rol = userdto.Id_Rol,
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


        /* Este método sirve para actualizar un FAQ dentro de la base de datos. */
        public async Task<bool?> EliminarUsuario(int idUsuarioParametrizado)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool? resultado = VerificarUsuario_ParaEliminar(idUsuarioParametrizado);

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultadoCorreo, que contiene la respuesta del método: -
             * ValidarUsuario_PorCorreo, es diferente a falso, y también, si en la -
             * variable: Resultado_Correo es diferente a 0. 
             * 
             * Quiere decir que ese usuario que se esta pasando por parametro si existe -
             * en la base de datos como tal. Entonces, si se podria actualizar la foto -
             * de perfil respectivamente. */
            if ((resultado != null || resultado != false) && resultado_usuario_eliminar != 0)
            {
                try
                {
                    int idUsuario_Empleado = resultado_usuario_eliminar;

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync(
                        "PROCED_Eliminar_Usuario",
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
        public async Task<UsuarioEntitie?>? ObtenerUsuario_PorCorreo(string correoParametrizado)
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
                        Correo = correoParametrizado
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
            }//Fin del try catch.

        }//Fin del método.


        /* Este método sirve para obtener un usuario por medio del correo en la base de datos. */
        public async Task<UsuarioEntitie?>? ObtenerUsuario_PorCedula(string cedulaParametrizada)
        {
            //Crea la conexión.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var usuario_Obtenido = await conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    "PROCED_Consultar_Usuario_X_Cedula",
                    new
                    {
                        Cedula = cedulaParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);

                /* Para que verificar si trajo el correo respectivo. 
                 * Además de permitir nulos a traves del simbolo: ? */
                string? verDatos = usuario_Obtenido?.Correo_Electronico;
                Debug.WriteLine("Datos que trajo: " + verDatos);

                /* Si el usuario que se obtuvo es nulo, quiere decir -
                 * que ese correo no existe dentro de la BD. Por lo -
                 * que se envia como respuesta un nulo. */
                if (usuario_Obtenido?.ToString() == null)
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
                     * respectivamente. */
                    string? usuarioId = usuario_Obtenido.Result?.Id.ToString();


                    /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                     * que ya existe un usuario con esa cédula, por lo que, en este caso se guarda -
                     * ese ID y se envia un falso al método: CrearUsuario. 
                     * 
                     * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                     * usuario como tal en la base de datos, y en consecuencia, este no permita -
                     * que se realice la creación de ese dato respectivamente. */
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
                     * resultado_cedula para que guarde ese ID, esto porque el resultado ya esta -
                     * indicando que no existe como tal (osea que es nulo).
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


        /* Este método sirve para obtener el ID de un usuario por medio del correo, esto por medio -
         * de la base de datos respectivamente. */
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
                 * respectivamente. */
                string? usuarioId = usuario_Obtenido.Result?.Id.ToString();


                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearUsuario. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
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
                 * resultado_correo para que guarde ese ID, esto porque el resultado ya esta -
                 * indicando que no existe como tal (osea que es nulo).
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


        /* Este método sirve para validar el usuario por medio del correo electrónico, esto por medio -
         * de la base de datos respectivamente. */
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
                Resultado_Correo = 0;
                Resultado_Correo_QR = 0;

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
                 * ese ID y se envia un falso a los métodos: EnviarCorreo, ActualizarContraseñaUsuario -
                 * y CrearCodigoQR. 
                 * 
                 * De modo que en dichos métodos, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, estos no permitan -
                 * que se realicen las acciones correspondientes. */
                if (usuarioId == null)
                {
                    Debug.WriteLine("Para ver la variable resultado_correo: " + resultado_Correo);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                    return false;
                }


                /* Si no entra quiere decir que no existe, por lo que lo guarda para que los -
                 * métodos que fueron mencionados anteriormente puedan saber sobre dicho aspecto. */
                resultado_Correo = usuario_Obtenido.Result!.Id;
                Resultado_Correo = usuario_Obtenido.Result!.Id;
                Resultado_Correo_QR = usuario_Obtenido.Result!.Id;

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


        /* Este método sirve para validar el usuario por medio del correo electrónico, esto por medio -
         * de la base de datos respectivamente. */
        public bool? VerificarUsuario_ParaActualizar(string cedulaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si el correo pasado por parametro es igual a nulo, entonces no puede seguir.
            if (cedulaParametrizada == null)
            {
                return false;
            }


            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_cedula_actualizar = 0;

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
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso a los métodos: EnviarCorreo, ActualizarContraseñaUsuario -
                 * y CrearCodigoQR. 
                 * 
                 * De modo que en dichos métodos, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, estos no permitan -
                 * que se realicen las acciones correspondientes. */
                if (usuarioId == null)
                {
                    Debug.WriteLine("Para ver la variable resultado_correo: " + resultado_cedula_actualizar);
                    Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);

                    conexionBD.Close();
                    Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                    return false;
                }


                /* Si no entra quiere decir que no existe, por lo que lo guarda para que los -
                 * métodos que fueron mencionados anteriormente puedan saber sobre dicho aspecto. */
                resultado_cedula_actualizar = usuario_Obtenido.Result!.Id;
                Debug.WriteLine("Para ver la variable resultado_correo: " + resultado_cedula_actualizar);

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


        /* Este método sirve para validar el usuario por medio del correo electrónico, esto por medio -
         * de la base de datos respectivamente. */
        public bool? VerificarUsuario_ParaEliminar(int? idUsuarioParametrizado)
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
                resultado_usuario_eliminar = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuario_Obtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Usuario_X_Id",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. 
                 * Además de verificar si trae algo la variable: "usuarioId" -
                 * respectivamente. */
                string? usuarioId = usuario_Obtenido.Result?.Id.ToString();

                Task<UsuarioEntitie?> usuarioID = usuario_Obtenido;

                Debug.WriteLine("Para ver la variable usuarioId: " + usuarioId);
                Debug.WriteLine("Para ver la variable usuarioId: " + usuarioID);



                /* Si el ID del usuario que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe un usuario con ese correo, por lo que, en este caso se guarda -
                 * ese ID y se envia un falso al método: CrearFAQ. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe un -
                 * usuario como tal en la base de datos, y en consecuencia, este no permita -
                 * que se realice la creación de ese dato respectivamente. */
                if (usuarioId != null)
                {
                    resultado_usuario_eliminar = usuario_Obtenido.Result!.Id;

                    Debug.WriteLine("Para ver la variable empleado_Obtenido: " + usuario_Obtenido);
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

                Debug.WriteLine("Para ver la variable faq_Obtenido: " + usuario_Obtenido);
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



        /* Este método sirve para obtener un código generado de forma aleatoria respectivamente. */
        private static string CodigoGenerado(int longitudParametrizada)
        {
            //Esto es la cadena que se usara para el código de recuperación.
            const string cadenaCaracteres = "WfjndFin*MBfu*HAdPNe$GTm(L5cf{Lc!94SLCZG+:7bY&U2+PnM(ER9Nurk.bZ&6xxzcw:A!*W?-xnA:]%u=yLmCSApY2K[5K##";

            /* Aqui lo que se hace es crear el contructor que ayudara hacer el código, y -
             * también el random que nos ayudara para poder utilizar cualquier caracter -
             * de la cadena antes mencionada. */
            StringBuilder constructorString = new StringBuilder();
            Random random = new Random();


            /* Aqui lo que se hace es verificar la longitud que se mando por parametro -
             * (osea 6), y en base a eso, entonces lo que va a hacer es utilizar cualquier -
             * caracter de la cadena, y con el constructor, agregarla a un nuevo texto. -
             * De forma que cuando se llegue a los 6 digitos, ya se tenga un código de -
             * recuperación totalmente nuevo y generado de forma aleatoria respectivamente. */
            for (int i = 0; i < longitudParametrizada; i++)
            {
                int index = random.Next(cadenaCaracteres.Length);
                constructorString.Append(cadenaCaracteres[index]);
            }


            /* Aqui lo que se hace es devolver el código de recuperación ya generado. */
            return constructorString.ToString();

        } //Fin del método.


        /* Este método sirve para encriptar una contraseña respectivamente. */
        private static string EncriptarContraseña(string contraseñaParametrizada)
        {
            //Primera parte de la encriptación:
            //byte[] llaveMaestra = Encoding.UTF8.GetBytes("AQUI SE DEBE PONER UNOS CARACTERES DE LA LLAVE");
            //byte[] vectorIniciacion = Encoding.UTF8.GetBytes("AQUI SE DEBE PONER UNOS CARACTERES PARA EL VECTOR, PERO DEBEN SER DE UN TAMAÑO DE 16.");
            //byte[] vectorInicializacion = Encoding.UTF8.GetBytes("me-t76S%].SC)Gwt"); //Tiene que ser 16 caracteres.            
            
            using var sha256 = SHA256.Create();
            
            byte[] llaveMaestra = Encoding.UTF8.GetBytes(",f4_Rj.BvNWi:LXt##{!gZyH}Vn170RX"); //Tiene que ser 32 caracteres.            
            byte[] llaveCodificada = sha256.ComputeHash(llaveMaestra);

            using Aes estandarAvanzadoEncriptacion = Aes.Create();
            estandarAvanzadoEncriptacion.Key = llaveCodificada;
            estandarAvanzadoEncriptacion.GenerateIV();


            //Segunda parte de la encriptación:
            using MemoryStream memoriaParaEncriptar = new MemoryStream();
            memoriaParaEncriptar.Write(estandarAvanzadoEncriptacion.IV, 0,
                estandarAvanzadoEncriptacion.IV.Length);


            ICryptoTransform encriptador = estandarAvanzadoEncriptacion.CreateEncryptor();
            using CryptoStream manejarEncriptacion = new CryptoStream(memoriaParaEncriptar, 
                encriptador, CryptoStreamMode.Write);


            byte[] bytesContraseña = Encoding.UTF8.GetBytes(contraseñaParametrizada);
            manejarEncriptacion.Write(bytesContraseña, 0, bytesContraseña.Length);
            manejarEncriptacion.FlushFinalBlock();


            //Tercera parte de la encriptación:
            return Convert.ToBase64String(memoriaParaEncriptar.ToArray());
        }


        /* Este método sirve para encriptar una contraseña respectivamente. */
        private static string DesencriptarContraseña(string contraseñaEncriptadaParametrizada)
        {
            //Primera parte de la encriptación:
            //byte[] llaveMaestra = Encoding.UTF8.GetBytes("AQUI SE DEBE PONER UNOS CARACTERES DE LA LLAVE");
            //byte[] vectorIniciacion = Encoding.UTF8.GetBytes("AQUI SE DEBE PONER UNOS CARACTERES PARA EL VECTOR, PERO DEBEN SER DE UN TAMAÑO DE 16.");
            //byte[] vectorInicializacion = Encoding.UTF8.GetBytes("me-t76S%].SC)Gwt"); //Tiene que ser 16 caracteres.


            using var sha256 = SHA256.Create();

            byte[] llaveMaestra = Encoding.UTF8.GetBytes(",f4_Rj.BvNWi:LXt##{!gZyH}Vn170RX");
            byte[] llaveCodificada = sha256.ComputeHash(llaveMaestra);
            byte[] bytesCifrados = Convert.FromBase64String(contraseñaEncriptadaParametrizada);


            if(bytesCifrados.Length < 16)
            {
                throw new ArgumentException("Error en el texto.");
            }


            using Aes estandarAvanzadoEncriptacion = Aes.Create();
            estandarAvanzadoEncriptacion.Key = llaveCodificada;
            
            byte[] vectorInicializacion = new byte[16];
            Array.Copy(bytesCifrados, 0, vectorInicializacion, 0, 16);
            estandarAvanzadoEncriptacion.IV = vectorInicializacion;


            ICryptoTransform desencriptador = estandarAvanzadoEncriptacion.CreateDecryptor();


            //Segunda parte de la encriptación:
            using MemoryStream memoriaParaDesencriptar = new MemoryStream(bytesCifrados, 16, bytesCifrados.Length - 16);
            using CryptoStream manejarDesencriptacion = new CryptoStream(memoriaParaDesencriptar, desencriptador, CryptoStreamMode.Read);
            using StreamReader leerDesencriptacion = new StreamReader(manejarDesencriptacion);

            string contraseñaDesencriptada = leerDesencriptacion.ReadToEnd();

            //Tercera parte de la encriptación:
            return contraseñaDesencriptada;

        }



        //|========================================| FIN DE LA CLASE |========================================|
    }
}
