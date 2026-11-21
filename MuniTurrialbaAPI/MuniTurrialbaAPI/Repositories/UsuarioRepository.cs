using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using QRCoder;
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

        //Variables globales:
        string CodigoRestablecimiento;

        int resultado_usuario_parteUno_crear;
        int resultado_usuario_parteDos_crear;
        int resultado_usuario_actualizar;
        int resultado_usuario_eliminar;        
        
        int resultado_contraseña_actualizar;
        int resultado_perfil_actualizar;
        int resultado_correo_enviar;
        int resultado_codigo_QR;


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

        /* Estos métodos sirven para crear, actualizar o eliminar a un usuario(a) dentro -
         * de la base de datos. */
        public async Task<bool?> CrearUsuario(UsuarioCreateDto usuarioDto, bool tipoParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            bool? primerResultado = VerificarUsuario_ParteUno_ParaCrear(usuarioDto.Cedula);
            bool? segundoResultado = VerificarUsuario_ParteDos_ParaCrear(usuarioDto.Correo_Electronico);

            if (primerResultado == null || segundoResultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en las variables: primerResultado y segundoResultado son diferentes a falso y en -
             * la variable: resultado_usuario_parteUno_crear y resultado_usuario_parteDos_crear son -
             * iguales a 0, quiere decir que ese usuario(a) no existe en la base de datos.
             * 
             * Por lo que entonces si se podria crear ese usuario(a) en la base de datos -
             * respectivamente. */
            if (primerResultado != false && resultado_usuario_parteUno_crear == 0 && segundoResultado != false && resultado_usuario_parteDos_crear == 0)
            {
                try
                {
                    /* Aqui lo que se hace es simplemente tomar la fecha y hora de registro -
                     * al momento que se esta usando este método. */
                    DateTime Fecha_Registro = DateTime.Now;

                    /* Aqui lo que se esta haciendo es enviar la contraseña para poder -
                     * encriptarla y mantener la seguridad del usuario(a) respectivamente. */
                    string contraseñaEncriptada = EncriptarContraseña(usuarioDto.Contraseña);
                    
                    /* Esta variable sirve para poder guardar la respuesta que se obtiene del -
                     * procedimiento almacenado respectivamente. */
                    var respuestaUsuario = "";

                    /* Aqui lo que se esta haciendo es validar el tipo de creación que se va -
                     * a realizar. Si es verdadero entonces quiere decir que ese usuario(a) -
                     * se esta registrando, por lo que seria la primera vez que ingresaria -
                     * a la aplicación, pero si resulta que da falso, entonces quiere decir -
                     * que ese usuario(a) se esta creando desde el apartado de control de -
                     * empleados respectivamente. */
                    if (tipoParametrizado == true)
                    {
                        /* Aqui simplemente se esta definiendo el departamento por defecto, -
                         * ya que en el registro de un usuario(a) no se coloca el departamento -
                         * como tal, esto por temas de seguridad. */
                        string nombreDepartamento = "Recursos Humanos";

                        //Para ejecutar el procedimiento almacenado.
                        var nuevoUsuario = await conexionBD.ExecuteAsync("PROCED_CrearUsuarios",
                            //Se coloca los parametros:
                            new
                            {
                                Nombre = usuarioDto.Nombre,
                                Apellido_1 = usuarioDto.Apellido_1,
                                Apellido_2 = usuarioDto.Apellido_2,
                                Edad = usuarioDto.Edad,
                                Cedula = usuarioDto.Cedula,
                                Telefono = usuarioDto.Telefono,
                                Correo_Electronico = usuarioDto.Correo_Electronico,
                                Contraseña = contraseñaEncriptada,
                                Fecha_Creacion = Fecha_Registro,
                                Id_Rol = usuarioDto.Id_Rol,
                                Leer = true,
                                Crear = true,
                                Actualizar = true,
                                Eliminar = true,
                                Descargar = true,
                                Compartir = true,
                                Activo = true,
                                Departamento = nombreDepartamento
                            },
                            commandType: System.Data.CommandType.StoredProcedure);

                        /* Aqui se guarda la respuesta del procedimiento almacenado respectivamente. */
                        respuestaUsuario = nuevoUsuario.ToString();
                    }
                    else
                    {
                        //Para ejecutar el procedimiento almacenado.
                        var nuevoUsuario = await conexionBD.ExecuteAsync("PROCED_CrearUsuario_Empleado",
                            //Se coloca los parametros:
                            new
                            {
                                Nombre = usuarioDto.Nombre,
                                Apellido_1 = usuarioDto.Apellido_1,
                                Apellido_2 = usuarioDto.Apellido_2,
                                Edad = usuarioDto.Edad,
                                Cedula = usuarioDto.Cedula,
                                Telefono = usuarioDto.Telefono,
                                Correo_Electronico = usuarioDto.Correo_Electronico,
                                Contraseña = contraseñaEncriptada,
                                Fecha_Creacion = Fecha_Registro,
                                Id_Rol = usuarioDto.Id_Rol,
                                Leer = true,
                                Crear = true,
                                Actualizar = true,
                                Eliminar = true,
                                Descargar = true,
                                Compartir = true
                            },
                            commandType: System.Data.CommandType.StoredProcedure);

                        /* Aqui se guarda la respuesta del procedimiento almacenado respectivamente. */
                        respuestaUsuario = nuevoUsuario.ToString();
                    }
                    

                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo crear el usuario(a), y de ese -
                     * modo mandar un error respectivamente. */
                    if (respuestaUsuario == null || respuestaUsuario.IsNullOrEmpty())
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
                    string lugarOcurrido = "CrearUsuario";

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

        public async Task<bool?> ActualizarUsuario(UsuarioCreateDto usuarioDto, string cedulaParametrizada)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            bool? resultado = VerificarUsuario_ParaActualizar(cedulaParametrizada);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_usuario_actualizar es diferente a 0, quiere decir que ese -
             * usuario(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese usuario(a) en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_usuario_actualizar != 0)
            {
                try
                {
                    /* Aqui lo que se esta haciendo es tener el ID del usuario(a) -
                     * para poder continuar con la actualización. */
                    int idUsuario = resultado_usuario_actualizar;

                    /* Aqui lo que se esta haciendo es enviar la nueva contraseña para poder -
                     * encriptarla y mantener la seguridad del usuario(a) respectivamente. */
                    string nuevaContraseñaEncriptada = EncriptarContraseña(usuarioDto.Contraseña);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_Usuario",
                        //Se coloca los parametros:
                        new
                        {
                            Nombre = usuarioDto.Nombre,
                            Apellido_1 = usuarioDto.Apellido_1,
                            Apellido_2 = usuarioDto.Apellido_2,
                            Edad = usuarioDto.Edad,
                            Cedula = usuarioDto.Cedula,
                            Contraseña = nuevaContraseñaEncriptada,
                            Telefono = usuarioDto.Telefono,
                            Correo_Electronico = usuarioDto.Correo_Electronico,
                            Id_Rol = usuarioDto.Id_Rol,
                            Id_Usuario = idUsuario
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar el usuario(a), y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaUsuario = resultadoActualizacion;
                    if (respuestaUsuario == 0)
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
                    string lugarOcurrido = "ActualizarUsuario";

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

        public async Task<bool?> ActualizarContraseñaUsuario(string contraseñaParametrizada, string correoParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            bool? resultado = VerificarCorreo(correoParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_contraseña_actualizar es diferente a 0, quiere decir que -
             * ese usuario(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese usuario(a) en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_contraseña_actualizar != 0)
            {
                try
                {
                    /* Aqui lo que se esta haciendo es enviar la nueva contraseña para poder -
                     * encriptarla y mantener la seguridad del usuario(a) respectivamente. */
                    string nuevaContraseñaEncriptada = EncriptarContraseña(contraseñaParametrizada);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_Contraseña_Usuario",
                        //Se coloca los parametros:
                        new
                        {
                            Correo_Electronico = correoParametrizado,
                            Contraseña = nuevaContraseñaEncriptada
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar la contraseña del -
                     * usuario(a), y de ese modo mandar un error respectivamente. */
                    var respuestaUsuario = resultadoActualizacion;
                    if (respuestaUsuario == 0)
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
                    string lugarOcurrido = "ActualizarContraseñaUsuario";

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

        public async Task<bool?> ActualizarFotoPerfil(string fotoParametrizado, string correoParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            bool? resultado = VerificarCorreo(correoParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_perfil_actualizar es diferente a 0, quiere decir que ese -
             * usuario(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria actualizar ese usuario(a) en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_perfil_actualizar != 0)
            {
                try
                {
                    /* Aqui lo que se hace es decodificar la foto que se paso por parametro, -
                     * a un arreglo de bytes. Esto porque en la BD se maneja un Varbinary, -
                     * entonces ocupa si o si, ese arreglo de bytes. 
                     *
                     * Además que en la documentación de Microsoft, mencionan que con -
                     * este comando: "Convert.FromBase64String(fotoParametrizado)", nos -
                     * devolvera un arreglo (o una lista) de bytes respectivamente. */
                    byte[] imagenPerfil = Convert.FromBase64String(fotoParametrizado);

                    //Para ejecutar el procedimiento almacenado.
                    var resultadoActualizacion = await conexionBD.ExecuteAsync("PROCED_Actualizar_ImagenPerfil_Usuario",
                        //Se coloca los parametros:
                        new
                        {
                            Correo_Electronico = correoParametrizado,
                            Imagen_Perfil = imagenPerfil
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo actualizar el usuario(a), y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaPerfil = resultadoActualizacion;
                    if (respuestaPerfil == 0)
                    {
                        return null;
                    }


                    /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                     * true como respuesta.*/
                    conexionBD.Close();

                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver la foto de perfil: " + imagenPerfil);
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
                    string lugarOcurrido = "ActualizarFotoPerfil";

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

        public async Task<bool?> EliminarUsuario(int idUsuarioParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            bool? resultado = VerificarUsuario_ParaEliminar(idUsuarioParametrizado);
            if (resultado == null)
            {
                return null;
            }

            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_usuario_eliminar es diferente a 0, quiere decir que ese -
             * usuario(a) si existe en la base de datos.
             * 
             * Por lo que entonces si se podria eliminar ese usuario(a) en la base -
             * de datos respectivamente. */
            if (resultado != false && resultado_usuario_eliminar != 0)
            {
                try
                {
                    //Para ejecutar el procedimiento almacenado.
                    var resultadoEliminacion = await conexionBD.ExecuteAsync("PROCED_Eliminar_Usuario",
                        //Se coloca los parametros:
                        new
                        {
                            Id_Usuario = idUsuarioParametrizado
                        },
                        commandType: System.Data.CommandType.StoredProcedure);


                    /* Aqui se valida la respuesta del procedimiento almacenado, esto -
                     * para saber si realmente no pudo eliminar el usuario(a), y de -
                     * ese modo mandar un error respectivamente. */
                    var respuestaUsuario = resultadoEliminacion;
                    if (respuestaUsuario == 0)
                    {
                        return null;
                    }

                    /* Se cierra la conexión por temas de buenas prácticas y se devuelve un -
                     * true como respuesta.*/
                    conexionBD.Close();

                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                     * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                     * Debug.WriteLine(resultadoEliminacion.ToString()); */

                    return true;
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
                    string lugarOcurrido = "EliminarUsuario";

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



        /* Estos métodos sirven para enviar correos electrónicos y crear códigos QR para los -
         * usuarios(as) respectivamente. */
        public bool? EnviarCorreo(string correoParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            bool? resultado = VerificarCorreo(correoParametrizado);
            if (resultado == null)
            {
                return null;
            }

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_correo_enviar es diferente a 0, quiere decir que ese usuario(a) -
             * si existe en la base de datos.
             * 
             * Por lo que entonces si se podria enviar el mensaje a ese correo electrónico -
             * del usuario(a) respectivamente. */
            if (resultado != false && resultado_correo_enviar != 0)
            {
                try
                {
                    /* Se resetea la variable para evitar problemas más adelante, - 
                     * esto por temas de buenas prácticas. */
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
                        /* Este es para colocar las credenciales del correo que -
                         * se usara en el FROM. Además de colocar su contraseña. */
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
                     * habia colocado en el parametro, para que finalmente así -
                     * se envie dicho mensaje y pueda recibir el código -
                     * respectivamente. 
                     * 
                     * Ya después de haberse enviado el correo entonces se limpiaria -
                     * la configuración para evitar problemas, además porque también -
                     * es una buena práctica al hacerlo. */
                    mailMessage.To.Add(correoParametrizado);
                    clienteSmtp.Send(mailMessage);
                    clienteSmtp.Dispose();

                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("¡Se envio el mensaje!"); */

                    return true;
                }
                catch (Exception error)
                {
                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error); */

                    //Se coloca el nombre del método en que ocurrio el error.
                    string lugarOcurrido = "EnviarCorreo";

                    //Se manda a registrar el error que acaba de ocurrir en la BD.
                    RegistrarError(error, lugarOcurrido);

                    return null;
                }
            }

            /* Se devuelve un falso si resulta que no existe ese correo electrónico -
             * de ese usuario(a) respectivamente. */
            return false;
        }

        public string? CrearCodigoQR(string nombreParametrizado, string apellidosParametrizados, string correoParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            bool? resultado = VerificarCorreo(correoParametrizado);
            if (resultado == null)
            {
                return null;
            }

            /* Si en la variable: resultado es diferente a falso y en la variable: -
             * resultado_codigo_QR es diferente a 0, quiere decir que ese usuario(a) -
             * si existe en la base de datos.
             * 
             * Por lo que entonces si se podria eliminar ese usuario(a) en la base de -
             * datos respectivamente. */
            if (resultado != false && resultado_codigo_QR != 0)
            {
                try
                {
                    /* Es un generador que nos permitira hacer el código -
                     * QR del usuario(a). En otras palabras, es como si -
                     * se estuviera usando un constructor básicamente. */
                    QRCodeGenerator generadorQR = new QRCodeGenerator();

                    /* Aqui lo que se esta haciendo es crear el código QR con los datos -
                     * que vamos a proporcionar en dicho código. Osea, basicamente seria -
                     * su estructura. De ahi que se esta usando los comandos QRCodeData y -
                     * el CreateQRCode que tiene el generadorQR respectivamente. 
                     *
                     * Además, también con ayuda del comando: "QRCodeGenerator.ECCLevel.H", -
                     * se estaria colocando un nivel H en dicho QR (siendo este el nivel más -
                     * alto), esto lo que significa es que ese nivel H nos daria un 30% más -
                     * de probabilidad de recuperar los datos que están en ese código QR. -
                     * Esto en dado caso que dicho código se llegue a dañar o incluso que -
                     * se llegue a corromper respectivamente, de ahi el porque se coloca -
                     * ese nivel. */
                    QRCodeData datosCodificar = generadorQR.CreateQrCode(
                        "Nombre: " + nombreParametrizado +
                        "\nApellidos: " + apellidosParametrizados +
                        "\nCorreo: " + correoParametrizado, QRCodeGenerator.ECCLevel.H);

                    /* Luego, aqui lo que se esta haciendo es una instancia de tipo: "PngByteQRCode" -
                     * donde se estara pasando los datos que se definieron para el código QR, para así -
                     * tener un arreglo (o lista) de bytes en un formato PNG. */
                    PngByteQRCode codigoQR = new PngByteQRCode(datosCodificar);

                    /* Y por ultimo, lo unico que faltaria es tomar la variable: "codigoQR", que contiene -
                     * el PngByteQRCode, y usar el método: GetGraphic(20) para poder crear finalmente el -
                     * código QR como una imagen de formato PNG. Y después de eso, se codificaria dicho -
                     * codigo QR en un formato de 64 caracteres, y se devolveria al usuario(a) respectivamente.
                     * 
                     * Eso si, se coloco un 20 en el método: GetGraphic(), porque representa la cantidad de -
                     * pixeles que se quiere dibujar para cada modulo en blanco y negro. Osea, básicamente -
                     * serian los pedacitos que conforman el código QR en blanco y negro. */
                    byte[] codigoListo = codigoQR.GetGraphic(20);
                    string modeloQR = Convert.ToBase64String(codigoListo);

                    return modeloQR;
                }
                catch (Exception error)
                {
                    /* Comandos para poder ver el resultado en las variables en un debug:
                     * Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                        "esto por el siguiente error: " + error); */

                    //Se coloca el nombre del método en que ocurrio el error.
                    string lugarOcurrido = "CrearCodigoQR";

                    //Se manda a registrar el error que acaba de ocurrir en la BD.
                    RegistrarError(error, lugarOcurrido);

                    return null;
                }
            }


            /* Se devuelve un falso si resulta que no existe ese correo electrónico -
             * de ese usuario(a) respectivamente. */
            return "false";
        }



        /* Estos métodos sirven para obtener a los usuarios(as) que están dentro de la -
         * base de datos respectivamente. */
        public async Task<UsuarioEntitie?>? ObtenerUsuario_PorCorreo(string correoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var usuarioObtenido = await conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    "PROCED_Consultar_Usuario_X_Correo",
                    //Se coloca los parametros:
                    new
                    {
                        Correo = correoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda los datos del usuario(a) que trae el procedimiento almacenado. */
                string? verDatos = usuarioObtenido?.Correo_Electronico;
                UsuarioEntitie? usuario = usuarioObtenido;


                /* Si el usuario(a) que se obtuvo desde la BD es igual a nulo, significa -
                 * que no existe ese usuario(a), por lo que en este caso se cierra la -
                 * conexión de la base de datos y se envia un nulo como respuesta -
                 * respectivamente. */
                if (usuario == null)
                {
                    //Para cerrar la conexión, esto por temas de buenas prácticas.
                    conexionBD.Close();

                    return null;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Datos del usuario(a): " + usuario);
                 * Debug.WriteLine("Para ver el correo del usuario(a): " + verDatos);
                 * 
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                //Para cerrar la conexión, esto por temas de buenas prácticas.
                conexionBD.Close();

                return usuarioObtenido;
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
                string lugarOcurrido = "ObtenerUsuario_PorCorreo";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public async Task<UsuarioEntitie?>? ObtenerUsuario_PorCedula(string cedulaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Para ejecutar el procedimiento almacenado.
                var usuarioObtenido = await conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    "PROCED_Consultar_Usuario_X_Cedula",
                    //Se coloca los parametros:
                    new
                    {
                        Cedula = cedulaParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda los datos del usuario(a) que trae el procedimiento almacenado. */
                string? verDatos = usuarioObtenido?.Correo_Electronico;
                UsuarioEntitie? usuario = usuarioObtenido;


                /* Si el usuario(a) que se obtuvo desde la BD es igual a nulo, significa -
                 * que no existe ese usuario(a), por lo que en este caso se cierra la -
                 * conexión de la base de datos y se envia un nulo como respuesta -
                 * respectivamente. */
                if (usuario == null)
                {
                    //Para cerrar la conexión, esto por temas de buenas prácticas.
                    conexionBD.Close();

                    return null;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Datos del usuario(a): " + usuario);
                 * Debug.WriteLine("Para ver el correo del usuario(a): " + verDatos);
                 * 
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                //Para cerrar la conexión, esto por temas de buenas prácticas.
                conexionBD.Close();

                return usuarioObtenido;
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
                string lugarOcurrido = "ObtenerUsuario_PorCedula";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Estos métodos sirven para verificar si esos usuarios(as) están o no en la base de datos.
         * Y ya con eso, dirigirlos a sus respectivos métodos, como son el de crear, actualizar o -
         * eliminar un usuario(a) respectivamente. */
        public Task<UsuarioEntitie?>? VerificarUsuario(string contraseñaParametrizada, string correoParametrizado)
        {
            /* Es para saber si ese usuario(a) existe en la BD. 
             * 
             * Si resulta que llega a dar nulo, quiere decir que ocurrio un error durante -
             * la verificación de ese usuario(a) y se enviaria un nulo para que el sistema -
             * pueda mandar el mensaje correspondiente. */
            Task<UsuarioEntitie?> resultado = ObtenerUsuario_PorCorreo(correoParametrizado)!;
            if (resultado.Result == null)
            {
                return null;
            }

            try
            {
                //Se obtiene el correo electrónico y la contraseña de ese usuario(a):
                string correoUsuarioBD = resultado.Result!.Correo_Electronico;

                /* Aqui se coloco el método de desencriptar la contraseña por que cuando -
                 * se registro ese usuario(a) en la BD, su contraseña se encripto por temas -
                 * de seguridad y buenas prácticas. Entonces por eso es necesario desencriptarla -
                 * para la validación que se encuentra más adelante. */
                string contraseñaUsuarioBD = DesencriptarContraseña(resultado.Result.Contraseña);


                /* Aqui lo que se hace es validar si el correo electrónico y la contraseña que se -
                 * pasaron por parametro son los mismos que se encuentran dentro de la base de datos. 
                 * 
                 * Si es así entonces devolvera al usuario(a), y en caso contrario devolveria un nulo -
                 * respectivamente. */
                if (correoUsuarioBD == correoParametrizado && contraseñaUsuarioBD == contraseñaParametrizada)
                {
                    return resultado;
                }

                return null;
            }
            catch (Exception error)
            {
                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("No se pudo realizar correctamente la operación, " + 
                 * "esto por el siguiente error: " + error); */

                //Se coloca el nombre del método en que ocurrio el error.
                string lugarOcurrido = "VerificarUsuario";

                //Se manda a registrar el error que acaba de ocurrir en la BD.
                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarUsuario_ParteUno_ParaCrear(string cedulaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if (cedulaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_usuario_parteUno_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuarioObtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_IdUsuario_X_Cedula",
                    new
                    {
                        Cedula = cedulaParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado.*/
                string? usuarioId = usuarioObtenido.Result?.Id.ToString();


                /* Si el ID del usuario(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese usuario(a), por lo que en este caso se guarda ese ID y se -
                 * envia un falso al método: CrearUsuario. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe ese -
                 * usuario(a) en la base de datos, y, en consecuencia, no permita la creación de -
                 * ese dato respectivamente. */
                if (usuarioId != null)
                {
                    resultado_usuario_parteUno_crear = usuarioObtenido.Result!.Id;

                    conexionBD.Close();

                    return false;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver la variable usuarioId: " + usuarioId);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                conexionBD.Close();

                return true;
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

                string lugarOcurrido = "VerificarUsuario_ParteUno_ParaCrear";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarUsuario_ParteDos_ParaCrear(string correoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si el correo pasado por parametro es distinto a nulo, entonces puede pasar.
            if (correoParametrizado == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_usuario_parteDos_crear = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuarioObtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_IdUsuario_X_Correo",
                    new
                    {
                        Correo_Electronico = correoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado.*/
                string? usuarioId = usuarioObtenido.Result?.Id.ToString();


                /* Si el ID del usuario(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese usuario(a), por lo que en este caso se guarda ese ID y se -
                 * envia un falso al método: CrearUsuario. 
                 * 
                 * De modo que en dicho método, puedan tener conocimiento de que si existe ese -
                 * usuario(a) en la base de datos, y, en consecuencia, no permita la creación de -
                 * ese dato respectivamente. */
                if (usuarioId != null)
                {
                    resultado_usuario_parteDos_crear = usuarioObtenido.Result!.Id;

                    conexionBD.Close();

                    return false;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver la variable usuarioId: " + usuarioId);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State); */

                conexionBD.Close();

                return true;
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

                string lugarOcurrido = "VerificarUsuario_ParteDos_ParaCrear";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarUsuario_ParaActualizar(string cedulaParametrizada)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if (cedulaParametrizada == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_usuario_actualizar = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuarioObtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_IdUsuario_X_Cedula",
                    new
                    {
                        Cedula = cedulaParametrizada
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? usuarioId = usuarioObtenido.Result?.Id.ToString();


                /* Si el ID del usuario(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese usuario(a), por lo que en este caso se guarda ese ID y se -
                 * envia un true al método: ActualizarUsuario. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * usuario(a) en la base de datos, y, en consecuencia, pueda permitir la actualización -
                 * de ese dato respectivamente. */
                if (usuarioId != null)
                {
                    resultado_usuario_actualizar = usuarioObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable usuarioId: " + usuarioId);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
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

                string lugarOcurrido = "VerificarUsuario_ParaActualizar";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public bool? VerificarUsuario_ParaEliminar(int idUsuarioParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_usuario_eliminar = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuarioObtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_Usuario_X_Id",
                    new
                    {
                        Id_Usuario = idUsuarioParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? usuarioId = usuarioObtenido.Result?.Id.ToString();
                Task<UsuarioEntitie?> usuario = usuarioObtenido;


                /* Si el ID del usuario(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese usuario(a), por lo que en este caso se guarda ese ID y se -
                 * envia un true al método: EliminarUsuario. 
                 * 
                 * De modo que en dicho método puedan tener conocimiento de que si existe ese -
                 * usuario(a) en la base de datos, y, en consecuencia, pueda permitir la eliminación -
                 * de ese dato respectivamente. */
                if (usuarioId != null)
                {
                    resultado_usuario_eliminar = usuarioObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug: 
                 * Debug.WriteLine("Para ver la variable usuarioId: " + usuarioId);
                 * Debug.WriteLine("Para ver la variable usuario: " + usuario);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
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

                string lugarOcurrido = "VerificarUsuario_ParaEliminar";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Estos métodos sirven para verificar si el código de recuperación o el correo -
         * electrónico de ese usuario(a) son validos respectivamente. */
        public bool? VerificarCodigo(string codigoParametrizado)
        {
            //Si el código pasado por parametro es distinto de nulo, puede pasar.
            if (codigoParametrizado == null)
            {
                return null;
            }

            /* Aqui lo que se hace es validar si el código de restablecimiento (o recuperación) -
             * que se paso por parametro, es el mismo que el código que se envio por el correo -
             * electrónico del usuario(a). 
             * 
             * Si resulta que no es el mismo código de restablecimiento, entonces se devolveria -
             * un falso, pero si es el caso contrario entonces devolveria un true como respuesta -
             * respectivamente. */
            if (codigoParametrizado == CodigoRestablecimiento)
            {
                return true;
            }

            return false;
        }

        public bool? VerificarCorreo(string correoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var conexionBD = CreateConnection();

            //Si el correo pasado por parametro es distinto de nulo, puede pasar.
            if (correoParametrizado == null)
            {
                return null;
            }

            try
            {
                //Primero hay que resetear esta variable para evitar una confusión más adelante.
                resultado_contraseña_actualizar = 0;
                resultado_perfil_actualizar = 0;
                resultado_correo_enviar = 0;
                resultado_codigo_QR = 0;

                //Para ejecutar el procedimiento almacenado.
                var usuarioObtenido = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_Consultar_IdUsuario_X_Correo",
                    new
                    {
                        Correo_Electronico = correoParametrizado
                    },
                    commandType: System.Data.CommandType.StoredProcedure);


                /* Guarda el ID que trae el procedimiento almacenado. */
                string? usuarioId = usuarioObtenido.Result?.Id.ToString();


                /* Si el ID del usuario(a) que se obtuvo desde la BD es distinto a nulo, significa -
                 * que ya existe ese usuario(a), por lo que en este caso se guarda ese ID y se envia -
                 * un true a los métodos: ActualizarContraseñaUsuario, ActualizarFotoPerfil, EnviarCorreo -
                 * y CrearCodigoQR.
                 * 
                 * De modo que en dichos métodos, puedan tener conocimiento de que si existe ese -
                 * usuario(a) en la base de datos, y, en consecuencia, permitan la continuación -
                 * de ese dato respectivamente. */
                if (usuarioId != null)
                {
                    resultado_contraseña_actualizar = usuarioObtenido.Result!.Id;
                    resultado_perfil_actualizar = usuarioObtenido.Result!.Id;
                    resultado_correo_enviar = usuarioObtenido.Result!.Id;
                    resultado_codigo_QR = usuarioObtenido.Result!.Id;

                    conexionBD.Close();

                    return true;
                }


                /* Comandos para poder ver el resultado en las variables en un debug:
                 * Debug.WriteLine("Para ver la variable usuarioId: " + usuarioId);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
                 * Debug.WriteLine("Para ver si la conexión sigue activa: " + conexionBD.State);
                 * Debug.WriteLine("Para ver si la conexión se cerro: " + conexionBD.State);
                 * 
                 * Debug.WriteLine("Para ver la variable usuarioObtenido: " + usuarioObtenido);
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

                string lugarOcurrido = "VerificarCorreo";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }



        /* Estos métodos sirven para registrar un error, generar un código QR aleatoriamente, encriptar -
         * y desencriptar una contraseña que podrian suceder durante la ejecución respectivamente. */
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


            //Aqui lo que se hace es devolver el código de recuperación ya generado.
            return constructorString.ToString();
        }

        private static string EncriptarContraseña(string contraseñaParametrizada)
        {
            //Primera parte de la encriptación:
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

        private static string DesencriptarContraseña(string contraseñaEncriptadaParametrizada)
        {
            //Primera parte de la encriptación:
            using var sha256 = SHA256.Create();

            byte[] llaveMaestra = Encoding.UTF8.GetBytes(",f4_Rj.BvNWi:LXt##{!gZyH}Vn170RX");
            byte[] llaveCodificada = sha256.ComputeHash(llaveMaestra);
            byte[] bytesCifrados = Convert.FromBase64String(contraseñaEncriptadaParametrizada);


            if(bytesCifrados.Length < 16)
            {
                throw new ArgumentException("Error durante la desencriptación.");
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
