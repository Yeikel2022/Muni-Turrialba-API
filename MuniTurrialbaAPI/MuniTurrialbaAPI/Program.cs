using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using MuniTurrialbaAPI.Repositories;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace MuniTurrialbaAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAuthorization();

            /*|==========| Zona para agregar los servicios |===========|*/
            builder.Services.AddSingleton<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddSingleton<IJwtRepository, JwtRepository>();
            builder.Services.AddSingleton<IPermisoRepository, PermisoRepository>();
            builder.Services.AddSingleton<IFAQRepository, FAQRepository>();
            builder.Services.AddSingleton<IEmpleadoRepository, EmpleadoRepository>();
            builder.Services.AddSingleton<IPermisoTiempoRepository, PermisoTiempoRepository>();
            builder.Services.AddSingleton<ISalarioRepository, SalarioRepository>();
            builder.Services.AddSingleton<IInicioSesionRepository, InicioSesionRepository>();
            builder.Services.AddSingleton<IRegistroArchivoRepository, RegistroArchivoRepository>();
            builder.Services.AddSingleton<IHistoricoRepository, HistoricoRepository>();


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            /* Lo que hace esto es que a diferencia del: "builder.Services.AddSwaggerGen();" -
             * este nos permite agregar la opción para autenticarnos con el token que generamos -
             * a la hora de iniciar sesión.
             * 
             * Ahora, también hay que recalcar que en si mismo tiene la misma funcionalidad de -
             * usar Swagger, pero este le agrega ese detalle extra, como si fuera una mejora -
             * básicamente. */
            builder.Services.AddSwaggerGen(c =>
            {
                //Esto es para agregar un titulo al API cuando se ejecuta:
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MuniTurrialbaAPI", Version = "v1" });

                /* Configuración para que el token generado con JWT, pueda ser usado en -
                 * Swagger respectivamente: */ 
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingrese el token JWT como: Bearer {token}",
                });

                /* Esta es una configuración de seguridad para que el API sepa que se debe -
                 * de utilizar el Bearer junto con el token respectivamente: */
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string [] {}
                    }
                });
            });


            /*|==========| Zona para agregar la configuración de JWT Token |===========|*/

            /* Aqui lo que se hace es obtener la llave que colocamos en el appsettings, -
             * y luego lo codificamos en base al formato UTF8. */
            var llaveMaestra = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

            /* Aqui lo que se hace es decirle al API que en la autenticación, se debe usar -
             * el esquema del JWT token, esto por medio del siguiente comando: -
             * "JwtBearerDefaults.AuthenticationScheme". 
             *
             * Una vez hecho eso, se añade el JwtBearer, el cual nos permitira poder hacer -
             * las configuraciones respectivas, como se visualiza con el: -
             * "options.TokenValidationParameters = new TokenValidationParameters", -
             * el cual nos ayudara a definir los parametros que nuestro token utilizara -
             * para poder ser validado, como pueden ser el emisor, la audiencia, el tiempo -
             * de vida, la llave del emisor, etc. */
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(llaveMaestra)
                    };
                });


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            /* Aqui se agrega esto para que el API pueda utilizar la autenticación. 
             *
             * NOTA: Si se pone dicho comando después del: "app.UseAuthorization();" -
             * o si no se pone directamente, entonces el API daria un error. */
            app.UseAuthentication();
            app.UseAuthorization();


            /*               |=============| RUTAS DEL API |=============|             */

            //                   |=============| GET |=============|

            //Ruta (tipo: GET) del API que sirve para traer todos los inicios de sesión que realizaron los usuarios(as):
            app.MapGet("/api/iniciosSesion", async (IInicioSesionRepository sesionRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es traer todos los registros de los usuarios(as) -
                     * que han iniciado sesión.
                     * 
                     * Ahora, si en la variable: "iniciosSesion" es nulo, entonces quiere -
                     * decir que no hay nada en la BD. Por lo tanto, se mandaria un mensaje -
                     * de error respectivamente. */
                    var iniciosSesion = await sesionRepo.ObtenerRegistros_InicioSesion();
                    if (iniciosSesion is null)
                    {
                        return Results.BadRequest("No se pudieron obtener los registros de inicios de sesión.");
                    }

                    return Results.Ok(iniciosSesion);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todos los empleados(as):
            app.MapGet("/api/empleados", async (IEmpleadoRepository empleadoRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aquí lo que se hace es traer todos los registros sobre los empleados(as) -
                     * que están dentro de la base de datos. 
                     * 
                     * Ahora, si en la variable: "empleados" es nulo, entonces quiere decir que -
                     * no hay nada en la BD. Por lo tanto, se mandaria un error respectivamente. */
                    var empleados = await empleadoRepo.ObtenerEmpleados();
                    if (empleados is null)
                    {
                        return Results.BadRequest("No se pudieron obtener los empleados(as).");
                    }

                    return Results.Ok(empleados);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todos los permisos de autorización:
            app.MapGet("/api/permisos/{correo:required}", async (string correo, 
                IPermisoRepository permisoRepo, IUsuarioRepository usuarioRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar el correo electrónico que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(correo))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correo);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece ese correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correo)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    /* Aqui lo que se hace es traer todos los permisos de autorización que -
                     * tiene el usuario(a).
                     * 
                     * Ahora, si en la variable: "permisos" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto, se manda un mensaje de error -
                     * respectivamente. */
                    var permisos = await permisoRepo.ObtenerPermisos_Usuario(idUsuario)!;
                    if (permisos is null)
                    {
                        return Results.BadRequest("No se pudieron obtener los permisos asociados al usuario(a) respectivamente.");
                    }

                    return Results.Ok(permisos);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todos los permisos de tiempo:
            app.MapGet("/api/permisosTiempo", async (IPermisoTiempoRepository tiempoRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es traer todos los registros de los permisos -
                     * de tiempo que están relacionados a los empleados(as).
                     * 
                     * Ahora, si en la variable: "permisosTiempo" es nulo, entonces quiere -
                     * decir que no hay nada en la BD. Por lo tanto, se mandaria un mensaje -
                     * de error respectivamente. */
                    var permisosTiempo = await tiempoRepo.ObtenerPermisosTiempo();
                    if (permisosTiempo is null)
                    {
                        return Results.BadRequest("No se pudieron obtener los permisos de tiempo.");
                    }

                    return Results.Ok(permisosTiempo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todos los salarios:
            app.MapGet("/api/salarios", async (ISalarioRepository salarioRepo, string tokenAcceso, 
                IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es traer todos los registros de los salarios que -
                     * están relacionados a los empleados(as).
                     * 
                     * Ahora, si en la variable: "salarios" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto, se mandaria un mensaje de -
                     * error respectivamente. */
                    var salarios = await salarioRepo.ObtenerSalarios();
                    if (salarios is null)
                    {
                        return Results.BadRequest("No se pudieron obtener los registros de los salarios.");
                    }

                    return Results.Ok(salarios);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todas las preguntas y respuestas:
            app.MapGet("/api/obtenerFAQs", async (IFAQRepository preguntasYrespuestasRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es traer todas las preguntas y respuestas que -
                     * están dentro de la base de datos.
                     * 
                     * Ahora, si en la variable: "fAQs" es nulo, entonces quiere decir que -
                     * no hay nada en la BD. Por lo tanto, se mandaria un mensaje de error -
                     * respectivamente. */
                    var fAQs = await preguntasYrespuestasRepo.ObtenerFAQs();
                    if (fAQs is null)
                    {
                        return Results.BadRequest("No se pudieron obtener las preguntas y respuestas.");
                    }

                    return Results.Ok(fAQs);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer un usuario(a) por medio de un correo:
            app.MapGet("/api/usuario/{correo:required}", async (string correo, string tokenAcceso,
                IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar el correo electrónico que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(correo))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correo);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora, si en la variable: "usuario" es nulo, entonces quiere decir que -
                     * no hay nada en la BD. Por lo tanto, se mandaria un mensaje de error -
                     * respectivamente. */
                    var usuario = await usuarioRepo.ObtenerUsuario_PorCorreo(correo)!;
                    if (usuario is null)
                    {
                        return Results.BadRequest("No se pudo obtener el usuario(a).");
                    }

                    return Results.Ok(usuario);
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para obtener el código QR del usuario(a):
            app.MapGet("/api/obtenerQR", (string nombre, string apellidos, string correo, 
                string tokenAcceso, IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar los datos del usuario(a) que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces se -
                     * mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrEmpty(nombre))
                    {
                        return Results.BadRequest("El nombre del usuario(a) es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(apellidos) || string.IsNullOrEmpty(apellidos))
                    {
                        return Results.BadRequest("Los apellidos del usuario(a) son necesarios.");
                    }


                    if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(correo))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correo);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    /* Aqui lo que se hace es traer el código QR que fue generado con la información -
                     * del usuario(a).
                     * 
                     * Ahora si resulta que da un nulo o un falso, entonces quiere decir que ocurrio -
                     * un error dentro del sistema o que está incorrecto. Por lo que en este caso se -
                     * mandaria un mensaje de error como respuesta. */
                    var codigoQR = usuarioRepo.CrearCodigoQR(nombre, apellidos, correo);
                    if (codigoQR == null)
                    {
                        return Results.BadRequest("No se pudo obtener el código QR del usuario(a).");
                    }

                    if (codigoQR.ToString() == "false")
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    return Results.Ok(codigoQR);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todos los registros de archivos:
            app.MapGet("/api/archivos", async (IRegistroArchivoRepository archivoRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es traer todos los registros de archivos que -
                     * realizaron los empleados(as).
                     * 
                     * Ahora, si en la variable: "archivos" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto, se mandaria un mensaje de -
                     * error respectivamente. */
                    var archivos = await archivoRepo.ObtenerRegistrosArchivos();
                    if (archivos is null)
                    {
                        return Results.BadRequest("No se pudieron obtener los registros de archivos.");
                    }

                    return Results.Ok(archivos);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todos los registros de archivos que fueron compartidos:
            app.MapGet("/api/obtenerArchivosCompartidos", async (string correoUsuario, string tokenAcceso, 
                IRegistroArchivoRepository archivoRepo, IJwtRepository jwtRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar el correo electrónico que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correoUsuario) || string.IsNullOrEmpty(correoUsuario))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoUsuario);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario y el empleado(a) en cuestión. 
                     * 
                     * Por lo que entonces se mandaria un mensaje de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es traer todos los registros de archivos que -
                     * que fueron compartidos por los empleados(as).
                     * 
                     * Ahora, si en la variable: "archivos" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto, se mandaria un mensaje de -
                     * error respectivamente. */
                    var archivos = await archivoRepo.ObtenerRegistrosArchivosCompartidos(idEmpleado);
                    if (archivos == null)
                    {
                        return Results.BadRequest("No se pudieron obtener los registros de archivos que fueron compartidos.");
                    }

                    return Results.Ok(archivos);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();



            //                   |=============| POST |=============|

            //Ruta (tipo: POST) del API que sirve para que el usuario(a) pueda iniciar sesión:
            app.MapPost("/api/iniciarsesion", async (ExtensionUsuarioCreateDto usuarioDto, IUsuarioRepository 
                usuarioRepo, IInicioSesionRepository sesionRepo, IJwtRepository jwtRepo, IPermisoRepository 
                permisoRepo, IEmpleadoRepository empleadoRepo) => 
            {
                /* Aqui lo que se hace es validar los datos del usuario(a) que se está -
                 * suministrando. Si resulta que da algún tipo de error, entonces se -
                 * mandaria su respectivo mensaje de error. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico) || string.IsNullOrEmpty(usuarioDto.Correo_Electronico))
                {
                    return Results.BadRequest("El correo electrónico es necesario.");
                }

                bool respuestaValidacion = ValidarCorreo(usuarioDto.Correo_Electronico);
                if (respuestaValidacion == false)
                {
                    return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                }


                if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || string.IsNullOrEmpty(usuarioDto.Contraseña))
                {
                    return Results.BadRequest("La contraseña es necesaria.");
                }

                if (usuarioDto.Contraseña.Trim().Length < 12)
                {
                    return Results.BadRequest("La contraseña no cumple con los requisitos minimos que fueron establecidos.");
                }


                /* Aqui lo que se hace es verificar si la contraseña y correo electrónico -
                 * que se brindaron, pertenecen algún usuario(a).
                 * 
                 * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                 * usuario(a) dentro de la base de datos. Por lo que se mandaria un mensaje -
                 * de error como respuesta. */
                var respuestaVerificacion_Usuario = usuarioRepo.VerificarUsuario(usuarioDto.Contraseña, usuarioDto.Correo_Electronico);
                if (respuestaVerificacion_Usuario == null)
                {
                    return Results.BadRequest("Ocurrio un error o simplemente el usuario(a) no existe en el sistema.");
                }


                /* Aqui lo que se hace es traer toda la información del usuario(a) a partir -
                 * de su ID.
                 * 
                 * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                 * usuario(a), y por ende, también no existiria como empleado(a) en cuestión. -
                 * Por lo que se mandaria un mensaje de error como respuesta. */
                Task<PermisoEntitie?> permisoUsuario = permisoRepo.ObtenerPermisos_Usuario(respuestaVerificacion_Usuario.Result!.Id)!;
                if (permisoUsuario.Result == null)
                {
                    return Results.BadRequest("Ocurrio un error durante el inicio de sesión.");
                }

                Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(respuestaVerificacion_Usuario.Result!.Id)!;
                if (Empleado.Result == null)
                {
                    return Results.BadRequest("Ocurrio un error durante el inicio de sesión.");
                }

                /* Poner lo del token.
                if (usuarioDto.Respuesta_Opcion == true)
                {
                    /* Aqui la idea es que si el usuario marco la opción: "MantenerSesión" -
                     * entonces que el sistema le devuelva el token de acceso y el token para -
                     * refrescar la sesión.

                    //Aqui es donde se va a poner todo dentro del token.
                    var claims_TokenGenerarB = new List<Claim>
                    {
                        new Claim("correo", respuestaValidacion_Usuario.Result!.Correo_Electronico),
                        new Claim("contraseña", respuestaValidacion_Usuario.Result!.Contraseña),
                        new Claim("rol", respuestaValidacion_Usuario.Result!.Id_Rol.ToString())
                    };

                    var respuestaTokenB = jwtRepo.crearTokenJWT(claims_TokenGenerarB);
                    var valorToken_Refrescado = jwtRepo.refrescarTokenJWT();
                    
                    return Results.Ok( new { 
                        TokenAcceso = respuestaTokenB,
                        TokenRefrescado = valorToken_Refrescado
                    });
                }*/

                /* Aqui es donde se va a poner todo dentro del token, osea en el payload -
                 * respectivamente. 
                 * 
                 * NOTA: En los claims solo aceptan el formato de texto, por lo que si se -
                 * usa otros formatos como el int o bool, hay que colocarles el ToString() -
                 * para así evitar que de errores. */
                var claims_TokenGenerar = new List<Claim>
                {
                    new Claim("nombre", respuestaVerificacion_Usuario.Result!.Nombre),
                    new Claim("primer_Apellido", respuestaVerificacion_Usuario.Result!.Apellido_1),
                    new Claim("segundo_Apellido", respuestaVerificacion_Usuario.Result!.Apellido_2),
                    
                    new Claim("correo", respuestaVerificacion_Usuario.Result!.Correo_Electronico),
                    new Claim("contraseña", respuestaVerificacion_Usuario.Result!.Contraseña),
                    new Claim("rol", respuestaVerificacion_Usuario.Result!.Id_Rol.ToString()),
                    new Claim("departamento", Empleado.Result.Departamento!),

                    new Claim("permiso_Leer", permisoUsuario.Result!.Leer.ToString()),
                    new Claim("permiso_Crear", permisoUsuario.Result!.Crear.ToString()),
                    new Claim("permiso_Actualizar", permisoUsuario.Result!.Actualizar.ToString()),
                    new Claim("permiso_Eliminar", permisoUsuario.Result!.Eliminar.ToString()),
                    new Claim("permiso_Descargar", permisoUsuario.Result!.Descargar.ToString()),
                    new Claim("permiso_Compartir", permisoUsuario.Result!.Compartir.ToString())
                };


                /* Aqui lo que se hace es crear una instancia para registrar el inicio de -
                 * sesión que realizo el usuario(a).
                 *
                 * Ahora, si resulta que da un nulo o un falso, entonces quiere decir -
                 * que ocurrio un error o que el usuario(a) no existe como tal. Por lo -
                 * que se mandaria un mensaje de error como respuesta. */
                InicioSesionCreateDto inicioSesionCreateDto = new InicioSesionCreateDto();

                inicioSesionCreateDto.Fecha_Inicio_Sesion = DateTime.Now;
                inicioSesionCreateDto.Hora = TimeOnly.FromDateTime(DateTime.Now);
                inicioSesionCreateDto.Ultima_Conexion = DateTime.Now;

                var respuestaInicioSesion = await sesionRepo.CrearRegistroInicioSesion(inicioSesionCreateDto, respuestaVerificacion_Usuario.Result!.Id);
                if (respuestaInicioSesion == null)
                {
                    return Results.BadRequest("Ocurrio un error durante el inicio de sesión.");
                }

                if (respuestaInicioSesion == false)
                {
                    return Results.BadRequest("No existe ese usuario(a) en el sistema.");
                }


                /* Y una vez colocado todo lo necesario en los claims, entonces se pasarian -
                 * al método: crearTokenJWT(), para que ahora así se pueda crear el token JWT -
                 * del usuario(a), y luego se pueda mandar en el result respectivamente. */
                var respuestaToken = jwtRepo.crearTokenJWT(claims_TokenGenerar);
                if (respuestaToken == null)
                {
                    return Results.BadRequest("Ocurrio un error durante el inicio de sesión.");
                }

                return Results.Created($"/api/iniciarsesion/{respuestaToken}", 
                    new { TokenAcceso = respuestaToken });
            });


            //Ruta (tipo: POST) del API que sirve para registrar a un nuevo usuario(a):
            app.MapPost("/api/registrarUsuarios", async (UsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar los datos del usuario(a) que se -
                 * está suministrando. Si resulta que da algún tipo de error, entonces -
                 * se mandaria su respectivo mensaje de error. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Nombre) || string.IsNullOrEmpty(usuarioDto.Nombre))
                {
                    return Results.BadRequest("El nombre es necesario.");
                }
                
                
                if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_1) || string.IsNullOrEmpty(usuarioDto.Apellido_1))
                {
                    return Results.BadRequest("El primer apellido es necesario.");
                }
                

                if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_2) || string.IsNullOrEmpty(usuarioDto.Apellido_2))
                {
                    return Results.BadRequest("El segundo apellido es necesario.");
                }
                
                
                if (string.IsNullOrWhiteSpace(usuarioDto.Edad.ToString()) || string.IsNullOrEmpty(usuarioDto.Edad.ToString()))
                {
                    return Results.BadRequest("La edad es necesaria.");
                }
                
                if (usuarioDto.Edad == 0 || usuarioDto.Edad > 99)
                {
                    return Results.BadRequest("La edad no puede ser un cero o pasar de los dos digitos respectivamente.");
                }
                
                
                if (string.IsNullOrWhiteSpace(usuarioDto.Cedula) || string.IsNullOrEmpty(usuarioDto.Cedula))
                {
                    return Results.BadRequest("La cédula es necesaria.");
                }

                if (usuarioDto.Cedula.Trim().Length > 12)
                {
                    return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                }


                if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico) || string.IsNullOrEmpty(usuarioDto.Correo_Electronico))
                {
                    return Results.BadRequest("El correo electrónico es necesario.");
                }

                bool respuestaValidacion = ValidarCorreo(usuarioDto.Correo_Electronico);
                if (respuestaValidacion == false)
                {
                    return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                }


                if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || string.IsNullOrEmpty(usuarioDto.Contraseña))
                {
                    return Results.BadRequest("La contraseña es necesaria.");
                }

                if (usuarioDto.Contraseña.Trim().Length < 12)
                {
                    return Results.BadRequest("La contraseña no cumple con los requisitos minimos que fueron establecidos.");
                }


                if (string.IsNullOrWhiteSpace(usuarioDto.Id_Rol.ToString()) || string.IsNullOrEmpty(usuarioDto.Id_Rol.ToString()))
                {
                    return Results.BadRequest("El rol es necesario.");
                }


                /* Aqui lo que se hace es crear un nuevo usuario(a). Ahora, si en -
                 * la variable: "nuevoUsuario" resulta ser nulo o falso, entonces -
                 * quiere decir que ocurrio un error o que ya existe un usuario(a). -
                 * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                var nuevoUsuario = await usuarioRepo.CrearUsuario(usuarioDto, true);
                if (nuevoUsuario == null)
                {
                    return Results.BadRequest("Ocurrio un error durante la creación del usuario(a).");
                }

                if (nuevoUsuario == false)
                {
                    return Results.BadRequest("Ya existe un usuario(a) con ese correo electrónico o cédula en el sistema.");
                }
                
                return Results.Created($"/api/registrarUsuarios/{nuevoUsuario}", 
                    new { Respuesta = nuevoUsuario });
            });


            //Ruta (tipo: POST) del API que sirve para crear un usuario(a):
            app.MapPost("/api/crearUsuarios", async (UsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo, string tokenAcceso, IJwtRepository jwtRepo,
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o de un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos del usuario(a) que se -
                     * está suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Nombre) || string.IsNullOrEmpty(usuarioDto.Nombre))
                    {
                        return Results.BadRequest("El nombre es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_1) || string.IsNullOrEmpty(usuarioDto.Apellido_1))
                    {
                        return Results.BadRequest("El primer apellido es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_2) || string.IsNullOrEmpty(usuarioDto.Apellido_2))
                    {
                        return Results.BadRequest("El segundo apellido es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Edad.ToString()) || string.IsNullOrEmpty(usuarioDto.Edad.ToString()))
                    {
                        return Results.BadRequest("La edad es necesaria.");
                    }

                    if (usuarioDto.Edad == 0 || usuarioDto.Edad > 99)
                    {
                        return Results.BadRequest("La edad no puede ser un cero o pasar de los dos digitos respectivamente.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Cedula) || string.IsNullOrEmpty(usuarioDto.Cedula))
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }

                    if (usuarioDto.Cedula.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico) || string.IsNullOrEmpty(usuarioDto.Correo_Electronico))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(usuarioDto.Correo_Electronico);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || string.IsNullOrEmpty(usuarioDto.Contraseña))
                    {
                        return Results.BadRequest("La contraseña es necesaria.");
                    }

                    if (usuarioDto.Contraseña.Trim().Length < 12)
                    {
                        return Results.BadRequest("La contraseña no cumple con los requisitos minimos que fueron establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Id_Rol.ToString()) || string.IsNullOrEmpty(usuarioDto.Id_Rol.ToString()))
                    {
                        return Results.BadRequest("El rol es necesario.");
                    }


                    /* Aqui lo que se hace es crear un nuevo usuario(a). Ahora, si en -
                     * la variable: "nuevoUsuario" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que ya existe un usuario(a). -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var nuevoUsuario = await usuarioRepo.CrearUsuario(usuarioDto, false);
                    if (nuevoUsuario == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la creación del usuario(a).");
                    }

                    if (nuevoUsuario == false)
                    {
                        return Results.BadRequest("Ya existe un usuario(a) con ese correo electrónico o cédula en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el usuario(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Crear Usuarios";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para crear un nuevo usuario(a) " +
                    "al sistema. " + "El cual es el siguiente: " + usuarioDto.Nombre + " " + usuarioDto.Apellido_1 + " " + usuarioDto.Apellido_2 + 
                    ", con cédula: " + usuarioDto.Cedula + " y correo electrónico: " + usuarioDto.Correo_Electronico + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Created($"/api/crearUsuarios/{nuevoUsuario}",
                        new { Respuesta = nuevoUsuario });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear un empleado(a):
            app.MapPost("/api/crearEmpleados", async (EmpleadoCreateDto empleadoDto,
                string correoEmpleado, string tokenAcceso, IEmpleadoRepository empleadoRepo, 
                IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo, 
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o de un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos del empleado(a) que se -
                     * esta suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correoEmpleado) || string.IsNullOrEmpty(correoEmpleado))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoEmpleado);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(empleadoDto.Activo.ToString()) || string.IsNullOrEmpty(empleadoDto.Activo.ToString()))
                    {
                        return Results.BadRequest("El activo es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(empleadoDto.Departamento) || string.IsNullOrEmpty(empleadoDto.Departamento))
                    {
                        return Results.BadRequest("El departamento es necesario.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;


                    /* Aqui lo que se hace es crear un nuevo empleado(a). Ahora, si en -
                     * la variable: "nuevoEmpleado" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que ya existe ese empleado(a). -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var nuevoEmpleado = await empleadoRepo.CrearEmpleado(empleadoDto, idUsuario);
                    if (nuevoEmpleado == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la creación del empleado(a).");
                    }

                    if (nuevoEmpleado == false)
                    {
                        return Results.BadRequest("Ya existe ese empleado(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Crear Empleados";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para crear un nuevo empleado(a) " +
                    "al sistema. " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " + 
                    Usuario!.Result!.Apellido_2 + ", con cédula: " + Usuario!.Result!.Cedula + " y correo electrónico: " + 
                    Usuario!.Result!.Correo_Electronico + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Created($"/api/crearEmpleados/{nuevoEmpleado}",
                        new { Respuesta = nuevoEmpleado });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear una pregunta y respuesta:
            app.MapPost("/api/crearFAQ", async (FAQCreateDto faqDto, IFAQRepository preguntasYrespuestasRepo,
                string tokenAcceso, IJwtRepository jwtRepo, IUsuarioRepository usuarioRepo, 
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o de un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos del FAQ que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(faqDto.Pregunta) || string.IsNullOrEmpty(faqDto.Pregunta))
                    {
                        return Results.BadRequest("La pregunta es necesaria.");
                    }


                    if (string.IsNullOrWhiteSpace(faqDto.Respuesta) || string.IsNullOrEmpty(faqDto.Respuesta))
                    {
                        return Results.BadRequest("La respuesta es necesaria.");
                    }


                    if (string.IsNullOrWhiteSpace(faqDto.Tipo_Prioridad) || string.IsNullOrEmpty(faqDto.Tipo_Prioridad))
                    {
                        return Results.BadRequest("El tipo de prioridad es necesario.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    string correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;

                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;


                    /* Aqui lo que se hace es crear una nueva pregunta y respuesta. Ahora, -
                     * si en la variable: "nuevoFAQ" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que ya existe esa pregunta y -
                     * respuesta. Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var nuevoFAQ = await preguntasYrespuestasRepo.CrearFAQ(faqDto, idUsuario);
                    if (nuevoFAQ == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la creación de la pregunta y respuesta.");
                    }

                    if (nuevoFAQ == false)
                    {
                        return Results.BadRequest("Ya existe esa pregunta y respuesta en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el usuario(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Crear FAQ";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para crear una nueva " +
                    "pregunta y respuesta, el cual es el siguiente: " + faqDto.Pregunta + ", con la respuesta: " + 
                    faqDto.Respuesta + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Created($"/api/crearFAQ/{nuevoFAQ}", 
                        new { Respuesta = nuevoFAQ });
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para enviar un correo de recuperación a los usuarios(as):
            app.MapPost("/api/enviarcorreo/{correo:required}", async (string correo, 
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el correo electrónico que se está -
                 * suministrando. Si resulta que da algún tipo de error, entonces -
                 * se mandaria su respectivo mensaje de error. */
                if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(correo))
                {
                    return Results.BadRequest("El correo electrónico es necesario.");
                }

                bool respuestaValidacion = ValidarCorreo(correo);
                if (respuestaValidacion == false)
                {
                    return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                }


                /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama al método -
                 * para enviar el correo electrónico. Ahora, si en la variable: "respuestaRecuperacion" -
                 * resulta ser nulo o falso, entonces quiere decir que ocurrio un error o que el correo -
                 * electrónico esta incorrecto. Por lo que en este caso se mandaria un mensaje de error -
                 * como respuesta */
                var respuestaRecuperacion = usuarioRepo.EnviarCorreo(correo);
                if (respuestaRecuperacion == null)
                {
                    return Results.BadRequest("No se pudo enviar el correo electrónico que fue proporcionado.");
                }

                if (respuestaRecuperacion == false)
                {
                    return Results.BadRequest("El correo electrónico que fue proporcionado no es válido.");
                }

                return Results.Ok(respuestaRecuperacion);
            });


            //Ruta (tipo: POST) del API que sirve para validar el código proporcionado:
            app.MapPost("/api/validarcodigo/{codigo:required}", (string codigo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el código de recuperación que se está -
                 * suministrando. Si resulta que da algún tipo de error, entonces se -
                 * mandaria su respectivo mensaje de error. */
                if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrEmpty(codigo))
                {
                    return Results.BadRequest("El código es necesario.");
                }

                /* Aqui lo que se hace es verificar el código de recuperación que brindo -
                 * el usuario(a).
                 * 
                 * Ahora si resulta que da un nulo o un falso, entonces quiere decir que -
                 * ocurrio un error dentro del sistema o que está incorrecto. Por lo que -
                 * en este caso se mandaria un mensaje de error como respuesta. */
                var respuestaRecuperacion = usuarioRepo.VerificarCodigo(codigo);
                if (respuestaRecuperacion == null)
                {
                    return Results.BadRequest("Ocurrio un error durante la verificación del código respectivamente.");
                }

                if (respuestaRecuperacion == false)
                {
                    return Results.BadRequest("El código que fue ingresado esta incorrecto.");
                }

                return Results.Ok(respuestaRecuperacion);
            });


            //Ruta (tipo: POST) del API que sirve para crear los permisos de tiempo:
            app.MapPost("/api/crearPermisosTiempo", async (PermisoTiempoCreateDto tiempoDto, 
                string cedulaUsuario, string tokenAcceso, IJwtRepository jwtRepo, 
                IPermisoTiempoRepository tiempoRepo, IEmpleadoRepository empleadoRepo, 
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o de un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos del empleado(a) que se esta -
                     * suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario) || string.IsNullOrEmpty(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }

                    if (cedulaUsuario.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Tipo_Permiso) || string.IsNullOrEmpty(tiempoDto.Tipo_Permiso))
                    {
                        return Results.BadRequest("El tipo de permiso no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Descripcion) || string.IsNullOrEmpty(tiempoDto.Descripcion))
                    {
                        return Results.BadRequest("La descripción no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Asignacion.ToString()) || string.IsNullOrEmpty(tiempoDto.Fecha_Asignacion.ToString()))
                    {
                        return Results.BadRequest("La fecha de asignación no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Finalizacion.ToString()) || string.IsNullOrEmpty(tiempoDto.Fecha_Finalizacion.ToString()))
                    {
                        return Results.BadRequest("La fecha de finalización no puede ser nula.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicha cédula.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe esa -
                     * cédula dentro de la base de datos, por ende, también no existiria el -
                     * usuario y el empleado(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                       return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }
                    
                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es crear un nuevo registro de permiso de tiempo. -
                     * Ahora, si en la variable: "nuevoPermisoTiempo" resulta ser nulo o falso, -
                     * entonces quiere decir que ocurrio un error o que ya existe un empleado(a) -
                     * con ese permiso de tiempo. Por lo tanto, se mandaria un mensaje de error -
                     * respectivamente. */
                    var nuevoPermisoTiempo = await tiempoRepo.CrearPermisosTiempo(tiempoDto, idEmpleado);
                    if (nuevoPermisoTiempo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la creación de los permisos de tiempo para el empleado(a).");
                    }

                    if (nuevoPermisoTiempo == false)
                    {
                        return Results.BadRequest("Ya existe un empleado(a) con ese permiso de tiempo respectivamente.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Crear Permisos Tiempo";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para crear un permiso de tiempo que esta " +
                    "asociado a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " +
                    Usuario!.Result!.Apellido_2 + " y con cédula: " + Usuario!.Result!.Cedula + " respectivamente.";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Created($"/api/crearPermisosTiempo/{nuevoPermisoTiempo}", 
                        new { Respuesta = nuevoPermisoTiempo });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear los salarios:
            app.MapPost("/api/crearSalarios", async (SalarioCreateDto salarioDto, string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, ISalarioRepository salarioRepo, 
                IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo,
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o de un administrador(a), y si no lo tiene, -
                 * entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos del empleado(a) que se -
                     * esta suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario) || string.IsNullOrEmpty(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }

                    if (cedulaUsuario.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(salarioDto.Fecha_Entrega) || string.IsNullOrEmpty(salarioDto.Fecha_Entrega))
                    {
                        return Results.BadRequest("La fecha de entrega no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(salarioDto.Salario.ToString()) || string.IsNullOrEmpty(salarioDto.Salario.ToString()))
                    {
                        return Results.BadRequest("El salario no puede ser nulo.");
                    }

                    if (Regex.IsMatch(salarioDto.Salario.ToString(), "@\"^\\d+.\\d{2}$"))
                    {
                        return Results.BadRequest("El salario no contiene los dos decimales que fueron establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(salarioDto.Descripcion) || string.IsNullOrEmpty(salarioDto.Descripcion))
                    {
                        return Results.BadRequest("La descripción no puede ser nula.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicha cédula.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe esa -
                     * cédula dentro de la base de datos, por ende, también no existiria el -
                     * usuario y el empleado(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es crear un nuevo registro de salario. Ahora, -
                     * si en la variable: "nuevoSalario" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que ya existe un empleado(a) con -
                     * ese salario. Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var nuevoSalario = await salarioRepo.CrearSalarios(salarioDto, idEmpleado);
                    if (nuevoSalario == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la creación del salario para el empleado(a).");
                    }

                    if (nuevoSalario == false)
                    {
                        return Results.BadRequest("Ya existe un empleado(a) con ese salario respectivamente.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Crear Salarios";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para crear un registro de salario que esta " +
                    "asociado a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " +
                    Usuario!.Result!.Apellido_2 + " y con cédula: " + Usuario!.Result!.Cedula + " respectivamente.";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Created($"/api/crearSalarios/{nuevoSalario}", 
                        new { Respuesta = nuevoSalario });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear los registros de archivos:
            app.MapPost("/api/crearArchivos", async (RegistroArchivoCreateDto archivoDto, string tokenAcceso, 
                IJwtRepository jwtRepo, IRegistroArchivoRepository archivoRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o de un empleado(a), y si no -
                 * lo tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar los datos del registro de archivo -
                     * que se está suministrando. Si resulta que da algún tipo de error, -
                     * entonces se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(archivoDto.Archivo.ToString()) || string.IsNullOrEmpty(archivoDto.Archivo.ToString()))
                    {
                        return Results.BadRequest("El archivo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Nombre_Archivo) || string.IsNullOrEmpty(archivoDto.Nombre_Archivo))
                    {
                        return Results.BadRequest("El nombre del archivo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Formato_Archivo) || string.IsNullOrEmpty(archivoDto.Formato_Archivo))
                    {
                        return Results.BadRequest("El formato del archivo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Nombre_Originario) || string.IsNullOrEmpty(archivoDto.Nombre_Originario))
                    {
                        return Results.BadRequest("El nombre del usuario originario(a) no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_1_Originario) || string.IsNullOrEmpty(archivoDto.Apellido_1_Originario))
                    {
                        return Results.BadRequest("El primer apellido del usuario originario(a) no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_2_Originario) || string.IsNullOrEmpty(archivoDto.Apellido_2_Originario))
                    {
                        return Results.BadRequest("El segundo apellido del usuario originario(a) no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(archivoDto.Correo_Electronico_Originario) || string.IsNullOrEmpty(archivoDto.Correo_Electronico_Originario))
                    {
                        return Results.BadRequest("El correo electrónico del usuario originario(a) no puede ser nulo.");
                    }

                    bool respuestaValidacion_Originario = ValidarCorreo(archivoDto.Correo_Electronico_Originario);
                    if (respuestaValidacion_Originario == false)
                    {
                        return Results.BadRequest("El correo electrónico del usuario originario(a) que fue proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Departamento_Originario) || string.IsNullOrEmpty(archivoDto.Departamento_Originario))
                    {
                        return Results.BadRequest("El departamento del usuario originario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Nombre_Destinatario) || string.IsNullOrEmpty(archivoDto.Nombre_Destinatario))
                    {
                        return Results.BadRequest("El nombre del usuario destinatario(a) no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_1_Destinatario) || string.IsNullOrEmpty(archivoDto.Apellido_1_Destinatario))
                    {
                        return Results.BadRequest("El primer apellido del usuario destinatario(a) no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_2_Destinatario) || string.IsNullOrEmpty(archivoDto.Apellido_2_Destinatario))
                    {
                        return Results.BadRequest("El segundo apellido del usuario destinatario(a) no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(archivoDto.Correo_Electronico_Destinatario) || string.IsNullOrEmpty(archivoDto.Correo_Electronico_Destinatario))
                    {
                        return Results.BadRequest("El correo electrónico del usuario destinatario(a) no puede ser nulo.");
                    }

                    bool respuestaValidacion_Destinatario = ValidarCorreo(archivoDto.Correo_Electronico_Destinatario);
                    if (respuestaValidacion_Destinatario == false)
                    {
                        return Results.BadRequest("El correo electrónico del usuario destinatario(a) que fue proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Departamento_Destinatario) || string.IsNullOrEmpty(archivoDto.Departamento_Destinatario))
                    {
                        return Results.BadRequest("El departamento del usuario destinatario(a) no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(archivoDto.Estado_Archivo) || string.IsNullOrEmpty(archivoDto.Estado_Archivo))
                    {
                        return Results.BadRequest("El estado del archivo no puede ser nulo.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario y el empleado(a) en cuestión. Por lo que se -
                     * mandaria un mensaje de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(archivoDto.Correo_Electronico_Destinatario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es crear un nuevo registro de archivo. Ahora, -
                     * si en la variable: "nuevoArchivo" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que ya existe un empleado(a) con -
                     * ese registro de archivo. Por lo tanto, se mandaria un mensaje de error -
                     * respectivamente. */
                    var nuevoArchivo = await archivoRepo.CrearRegistrosArchivos(archivoDto, idEmpleado);
                    if (nuevoArchivo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la creación del registro de archivo.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Crear Archivos";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para crear un registro de un archivo que " +
                    "esta asociado a un empleado destinatario(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 +
                    " " + Usuario!.Result!.Apellido_2 + ", con cédula: " + Usuario!.Result!.Cedula + " y correo electrónico: " + Usuario!.Result!.Correo_Electronico 
                    + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Created($"/api/crearArchivos/{nuevoArchivo}", 
                        new { Respuesta = nuevoArchivo });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();



            //                    |=============| PUT |=============|

            //Ruta (tipo: PUT) del API que sirve para actualizar las contraseñas de los usuarios(as):
            app.MapPut("/api/actualizarcontraseña", async (ExtensionUsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar los datos del usuario(a) que se esta -
                 * suministrando. Si resulta que da algún tipo de error, entonces se .
                 * mandaria su respectivo mensaje de error. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico) || string.IsNullOrEmpty(usuarioDto.Correo_Electronico))
                {
                    return Results.BadRequest("El correo electrónico es necesario.");
                }

                bool respuestaValidacion = ValidarCorreo(usuarioDto.Correo_Electronico);
                if (respuestaValidacion == false)
                {
                    return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                }


                if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || string.IsNullOrEmpty(usuarioDto.Contraseña))
                {
                    return Results.BadRequest("La contraseña es necesaria.");
                }

                if (usuarioDto.Contraseña.Trim().Length < 12)
                {
                    return Results.BadRequest("La contraseña no cumple con los requisitos minimos que fueron establecidos.");
                }


                /* Aqui lo que se hace es actualizar la contraseña de un usuario(a). -
                 * Ahora, si en la variable: "respuestaActualizacion" resulta ser -
                 * nulo o falso, entonces quiere decir que ocurrio un error o que -
                 * no existe ese usuario(a). Por lo tanto, se mandaria un mensaje -
                 * de error respectivamente. */
                var respuestaActualizacion = await usuarioRepo.ActualizarContraseñaUsuario(usuarioDto.Contraseña, usuarioDto.Correo_Electronico);
                if (respuestaActualizacion == null)
                {
                    return Results.BadRequest("Ocurrio un error durante la actualización de la contraseña.");
                }

                if (respuestaActualizacion == false)
                {
                    return Results.BadRequest("No existe ese usuario(a) en el sistema.");
                }

                return Results.Ok(respuestaActualizacion);
            });


            //Ruta (tipo: PUT) del API que sirve para actualizar los usuarios(as):
            app.MapPut("/api/actualizarUsuario", async (UsuarioCreateDto usuarioDto, 
                IUsuarioRepository usuarioRepo, string tokenAcceso, string cedula, 
                IJwtRepository jwtRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o de un administrador(a), y si no lo tiene, -
                 * entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos del usuario(a) que se esta -
                     * suministrando. Si resulta que da algún tipo de error, entonces se -
                     * mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrEmpty(cedula))
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }

                    if (cedula.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Nombre) || string.IsNullOrEmpty(usuarioDto.Nombre))
                    {
                        return Results.BadRequest("El nombre es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_1) || string.IsNullOrEmpty(usuarioDto.Apellido_1))
                    {
                        return Results.BadRequest("El primer apellido es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_2) || string.IsNullOrEmpty(usuarioDto.Apellido_2))
                    {
                        return Results.BadRequest("El segundo apellido es necesario.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Edad.ToString()) || string.IsNullOrEmpty(usuarioDto.Edad.ToString()))
                    {
                        return Results.BadRequest("La edad es necesaria.");
                    }

                    if (usuarioDto.Edad == 0 || usuarioDto.Edad > 99)
                    {
                        return Results.BadRequest("La edad no puede ser un cero o pasar de los dos digitos respectivamente.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(usuarioDto.Cedula) || string.IsNullOrEmpty(usuarioDto.Cedula))
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }

                    if (usuarioDto.Cedula.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico) || string.IsNullOrEmpty(usuarioDto.Correo_Electronico))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(usuarioDto.Correo_Electronico);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || string.IsNullOrEmpty(usuarioDto.Contraseña))
                    {
                        return Results.BadRequest("La contraseña es necesaria.");
                    }

                    if (usuarioDto.Contraseña.Trim().Length < 12)
                    {
                        return Results.BadRequest("La contraseña no cumple con los requisitos minimos que fueron establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(usuarioDto.Id_Rol.ToString()) || string.IsNullOrEmpty(usuarioDto.Id_Rol.ToString()))
                    {
                        return Results.BadRequest("El rol es necesario.");
                    }


                    /* Aqui lo que se hace es actualizar un usuario(a). Ahora, si en -
                     * la variable: "resultadoUsuario" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese usuario(a). -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoUsuario = await usuarioRepo.ActualizarUsuario(usuarioDto, cedula);
                    if (resultadoUsuario == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización del usuario(a).");
                    }

                    if (resultadoUsuario == false)
                    {
                        return Results.BadRequest("No existe ese usuario(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el usuario(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar Usuario";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar un usuario(a) " +
                    "del sistema. " + "El cual es el siguiente: " + usuarioDto.Nombre + " " + usuarioDto.Apellido_1 + " " +
                    usuarioDto.Apellido_2 + " y con cédula: " + usuarioDto.Cedula + " respectivamente.";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoUsuario);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar los permisos de autorización:
            app.MapPut("/api/actualizarPermisos", async (PermisoCreateDto permisoDto, string correoUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, IPermisoRepository permisoRepo, 
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador(a), y si no lo tiene, entonces no lo dejara pasar -
                 * respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1)
                {
                    /* Aqui lo que se hace es validar los datos del usuario(a) que se esta -
                     * suministrando. Si resulta que da algún tipo de error, entonces se -
                     * mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correoUsuario) || string.IsNullOrEmpty(correoUsuario))
                    {
                        return Results.BadRequest("El correo electrónico es necesario.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoUsuario);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(permisoDto.Leer.ToString()) || string.IsNullOrEmpty(permisoDto.Leer.ToString()))
                    {
                        return Results.BadRequest("El permiso que esta relacionado a leer no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(permisoDto.Crear.ToString()) || string.IsNullOrEmpty(permisoDto.Crear.ToString()))
                    {
                        return Results.BadRequest("El permiso que esta relacionado a crear no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(permisoDto.Actualizar.ToString()) || string.IsNullOrEmpty(permisoDto.Actualizar.ToString()))
                    {
                        return Results.BadRequest("El permiso que esta relacionado a actualizar no puede ser nulo.");
                    }

                    
                    if (string.IsNullOrWhiteSpace(permisoDto.Eliminar.ToString()) || string.IsNullOrEmpty(permisoDto.Eliminar.ToString()))
                    {
                        return Results.BadRequest("El permiso que esta relacionado a eliminar no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(permisoDto.Descargar.ToString()) || string.IsNullOrEmpty(permisoDto.Descargar.ToString()))
                    {
                        return Results.BadRequest("El permiso que esta relacionado a descargar no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(permisoDto.Compartir.ToString()) || string.IsNullOrEmpty(permisoDto.Compartir.ToString()))
                    {
                        return Results.BadRequest("El permiso que esta relacionado a compartir no puede ser nulo.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;


                    /* Aqui lo que se hace es actualizar los permisos de autorización del usuario(a). -
                     * Ahora, si en la variable: "resultadoPermiso" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese usuario(a). Por lo tanto, -
                     * se mandaria un mensaje de error respectivamente. */
                    var resultadoPermiso = await permisoRepo.ActualizarPermisos_Usuario(permisoDto, idUsuario);
                    if (resultadoPermiso == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización de los permisos del usuario(a).");
                    }

                    if (resultadoPermiso == false)
                    {
                        return Results.BadRequest("No existe ese usuario(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el usuario(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar Permisos";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar los permisos que estan " +
                    "asociados a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " +
                    Usuario!.Result!.Apellido_2 + ", con cédula: " + Usuario!.Result!.Cedula + " y correo electrónico: " + 
                    Usuario!.Result!.Correo_Electronico + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoPermiso);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar los permisos de tiempo:
            app.MapPut("/api/actualizarPermisosTiempo", async (PermisoTiempoCreateDto tiempoDto, string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, IPermisoTiempoRepository tiempoRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador(a), y si no lo tiene, entonces no lo dejara -
                 * pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1)
                {
                    /* Aqui lo que se hace es validar los datos del empleado(a) que se -
                     * esta suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario) || string.IsNullOrEmpty(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula no puede ser nula.");
                    }

                    if (cedulaUsuario.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Tipo_Permiso) || string.IsNullOrEmpty(tiempoDto.Tipo_Permiso))
                    {
                        return Results.BadRequest("El tipo de permiso no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Descripcion) || string.IsNullOrEmpty(tiempoDto.Descripcion))
                    {
                        return Results.BadRequest("La descripción no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Asignacion.ToString()) || string.IsNullOrEmpty(tiempoDto.Fecha_Asignacion.ToString()))
                    {
                        return Results.BadRequest("La fecha de asignación no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Finalizacion.ToString()) || string.IsNullOrEmpty(tiempoDto.Fecha_Finalizacion.ToString()))
                    {
                        return Results.BadRequest("La fecha de finalización no puede ser nula.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicha cédula.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe esa -
                     * cédula dentro de la base de datos, por ende, también no existiria el usuario -
                     * y el empleado(a) en cuestión. Por lo que se mandaria un mensaje de error -
                     * como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es actualizar el permiso de tiempo del empleado(a). -
                     * Ahora, si en la variable: "resultadoPermisoTiempo" resulta ser nulo o falso, -
                     * entonces quiere decir que ocurrio un error o que no existe ese empleado(a). -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoPermisoTiempo = await tiempoRepo.ActualizarPermisosTiempo(tiempoDto, idEmpleado);
                    if (resultadoPermisoTiempo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización del permiso de tiempo.");
                    }

                    if (resultadoPermisoTiempo == false)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar Permisos Tiempo";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar un permiso de tiempo que esta " +
                    "asociado a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " +
                    Usuario!.Result!.Apellido_2 + " y con cédula: " + Usuario!.Result!.Cedula + " respectivamente.";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoPermisoTiempo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar los salarios:
            app.MapPut("/api/actualizarSalarios", async (SalarioCreateDto salarioDto, string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, ISalarioRepository salarioRepo, 
                IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo,
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador(a), y si no lo tiene, entonces no lo dejara pasar -
                 * respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1)
                {
                    /* Aqui lo que se hace es validar los datos del empleado(a) que se -
                     * esta suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario) || string.IsNullOrEmpty(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula no puede ser nula.");
                    }

                    if (cedulaUsuario.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(salarioDto.Fecha_Entrega) || string.IsNullOrEmpty(salarioDto.Fecha_Entrega))
                    {
                        return Results.BadRequest("La fecha de entrega no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(salarioDto.Salario.ToString()) || string.IsNullOrEmpty(salarioDto.Salario.ToString()))
                    {
                        return Results.BadRequest("El salario no puede ser nulo.");
                    }

                    if (Regex.IsMatch(salarioDto.Salario.ToString(), "@\"^\\d+\\.\\d{2}$\""))
                    {
                        return Results.BadRequest("El salario no contiene los dos decimales que fueron establecidos.");
                    }


                    if (string.IsNullOrWhiteSpace(salarioDto.Descripcion) || string.IsNullOrEmpty(salarioDto.Descripcion))
                    {
                        return Results.BadRequest("La descripción no puede ser nula.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicha cédula.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe esa -
                     * cédula dentro de la base de datos, por ende, también no existiria el -
                     * usuario y el empleado(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es actualizar el salario del empleado(a). Ahora, -
                     * si en la variable: "resultadoSalario" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese empleado(a). Por lo -
                     * tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoSalario = await salarioRepo.ActualizarSalarios(salarioDto, idEmpleado);
                    if (resultadoSalario == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización del salario.");
                    }

                    if (resultadoSalario == false)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar Salarios";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar un salario que esta " +
                    "asociado a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " +
                    Usuario!.Result!.Apellido_2 + " y con cédula: " + Usuario!.Result!.Cedula + " respectivamente.";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoSalario);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar la foto de perfi:
            app.MapPut("/api/cambiarFoto", async (string tokenAcceso, HttpRequest peticion,
                IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se esta haciendo es que con la variable: "peticion", nos -
                     * ayudaria a obtener la solicitud HTTP que fue solicitado por el -
                     * usuario(a). Luego de eso, con esa variable podriamos leer el -
                     * contenido de la solicitud a través del comando: ReadFormAsync(), -
                     * y con eso devolver ese contenido a través de una colección: -
                     * Task<IFormCollection>.
                     * 
                     *
                     * Ahora, gracias a esa colección podriamos extraer la imagen que -
                     * esta almacenada en el campo: "Imagen_Perfil", de forma que ahora -
                     * se pueda hacer el procedimiento necesario para cambiar la imagen -
                     * de perfil. Sin embargo, si resulta dar un nulo, entonces se -
                     * mandaria un mensaje de error respectivamente. */
                    var archivoImagen = await peticion.ReadFormAsync();
                    if (archivoImagen == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización de la foto de perfil.");
                    }

                    var imagenPerfil = archivoImagen["Imagen_Perfil"].ToString();


                    /* Aqui lo que se hace es validar la imagen de perfil del usuario(a) -
                     * que se está suministrando. Si resulta que da algún tipo de error, -
                     * entonces se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(imagenPerfil) || string.IsNullOrEmpty(imagenPerfil))
                    {
                        return Results.BadRequest("La foto de perfil del usuario(a) no puede ser nulo.");
                    }

                    /* Aqui lo que se hace es obtener el claim que tiene como nombre: "correo" y -
                     * guardarlo en la variable: "correoUsuario". Esto se hace porque se necesita -
                     * enviar ese correo del usuario(a) para así ver si se puede cambiar o no la -
                     * foto de perfil. */
                    var correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;

                    /* Aqui lo que se hace es actualizar la foto de perfil del usuario(a). Ahora, -
                     * si en la variable: "resultadoPerfil" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese usuario(a). Por lo -
                     * tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoPerfil = await usuarioRepo.ActualizarFotoPerfil(imagenPerfil, correoUsuario);
                    if (resultadoPerfil == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización de la foto de perfil.");
                    }

                    if (resultadoPerfil == false)
                    {
                        return Results.BadRequest("No existe ese usuario(a) en el sistema.");
                    }

                    return Results.Ok(resultadoPerfil);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar las preguntas y respuestas:
            app.MapPut("/api/actualizarFAQ", async (FAQCreateDto faqDto, string preguntaActual, 
                IFAQRepository preguntasYrespuestasRepo, string tokenAcceso, IJwtRepository jwtRepo,
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos de la pregunta y respuesta -
                     * que se está suministrando. Si resulta que da algún tipo de error, -
                     * entonces se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(preguntaActual) || string.IsNullOrEmpty(preguntaActual))
                    {
                        return Results.BadRequest("La pregunta actual no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(faqDto.Pregunta) || string.IsNullOrEmpty(faqDto.Pregunta))
                    {
                        return Results.BadRequest("La pregunta no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(faqDto.Respuesta) || string.IsNullOrEmpty(faqDto.Respuesta))
                    {
                        return Results.BadRequest("La respuesta no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(faqDto.Tipo_Prioridad) || string.IsNullOrEmpty(faqDto.Tipo_Prioridad))
                    {
                        return Results.BadRequest("El tipo de prioridad no puede ser nulo.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    string correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;

                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;


                    /* Aqui lo que se hace es actualizar la pregunta y respuesta. Ahora, si en -
                     * la variable: "resultadoFAQ" resulta ser nulo o falso, entonces quiere -
                     * decir que ocurrio un error o que no existe esa pregunta y respuesta. -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoFAQ = await preguntasYrespuestasRepo.ActualizarFAQ(faqDto, preguntaActual, idUsuario);
                    if (resultadoFAQ == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización del FAQ.");
                    }

                    if (resultadoFAQ == false)
                    {
                        return Results.BadRequest("No existe ese FAQ en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar FAQ";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar una pregunta y respuesta, " +
                    "el cual es el siguiente: " + faqDto.Pregunta + ", con la respuesta: " + faqDto.Respuesta + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoFAQ);
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar a los empleados(as):
            app.MapPut("/api/actualizarEmpleado", async (EmpleadoCreateDto empleadoDto,
                IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo,
                string correoEmpleado, string tokenAcceso, IJwtRepository jwtRepo,
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar los datos del empleado(a) que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces se -
                     * mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correoEmpleado) || string.IsNullOrEmpty(correoEmpleado))
                    {
                        return Results.BadRequest("El correo electrónico no puede ser nulo.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoEmpleado);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(empleadoDto.Activo.ToString()) || string.IsNullOrEmpty(empleadoDto.Activo.ToString()))
                    {
                        return Results.BadRequest("El activo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(empleadoDto.Departamento) || string.IsNullOrEmpty(empleadoDto.Departamento))
                    {
                        return Results.BadRequest("El departamento no puede ser nulo.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;


                    /* Aqui lo que se hace es actualizar un empleado(a). Ahora, si en -
                     * la variable: "resultadoEmpleado" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese empleado(a). -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoEmpleado = await empleadoRepo.ActualizarEmpleado(empleadoDto, idUsuario);
                    if (resultadoEmpleado == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización del empleado(a).");
                    }

                    if (resultadoEmpleado == false)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el usuario(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar Empleado";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar un empleado(a) del sistema. " + 
                    "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " + Usuario!.Result!.Apellido_2 + 
                    ", con cédula: " + Usuario!.Result!.Cedula + " y correo electrónico: " + Usuario!.Result!.Correo_Electronico + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoEmpleado);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar los registros de archivos:
            app.MapPut("/api/actualizarArchivo", async (RegistroArchivoCreateDto archivoDto, 
                string correoEmpleado, string tokenAcceso, IEmpleadoRepository empleadoRepo, 
                IJwtRepository jwtRepo, IRegistroArchivoRepository archivoRepo, 
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar los datos del registro del archivo -
                     * que se está suministrando. Si resulta que da algún tipo de error, -
                     * entonces se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correoEmpleado) || string.IsNullOrEmpty(correoEmpleado))
                    {
                        return Results.BadRequest("El correo electrónico no puede ser nulo.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoEmpleado);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Archivo.ToString()) || string.IsNullOrEmpty(archivoDto.Archivo.ToString()))
                    {
                        return Results.BadRequest("El archivo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Nombre_Archivo) || string.IsNullOrEmpty(archivoDto.Nombre_Archivo))
                    {
                        return Results.BadRequest("El nombre del archivo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Formato_Archivo) || string.IsNullOrEmpty(archivoDto.Formato_Archivo))
                    {
                        return Results.BadRequest("El formato del archivo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Fecha_Subida) || string.IsNullOrEmpty(archivoDto.Fecha_Subida))
                    {
                        return Results.BadRequest("La fecha de subida no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Nombre_Originario) || string.IsNullOrEmpty(archivoDto.Nombre_Originario))
                    {
                        return Results.BadRequest("El nombre del usuario originario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_1_Originario) || string.IsNullOrEmpty(archivoDto.Apellido_1_Originario))
                    {
                        return Results.BadRequest("El primer apellido del usuario originario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_2_Originario) || string.IsNullOrEmpty(archivoDto.Apellido_2_Originario))
                    {
                        return Results.BadRequest("El segundo apellido del usuario originario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Correo_Electronico_Originario) || string.IsNullOrEmpty(archivoDto.Correo_Electronico_Originario))
                    {
                        return Results.BadRequest("El correo electrónico del usuario originario(a) no puede ser nulo.");
                    }

                    bool respuestaValidacion_Originario = ValidarCorreo(archivoDto.Correo_Electronico_Originario);
                    if (respuestaValidacion_Originario == false)
                    {
                        return Results.BadRequest("El correo electrónico del usuario originario(a) que fue proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Departamento_Originario) || string.IsNullOrEmpty(archivoDto.Departamento_Originario))
                    {
                        return Results.BadRequest("El departamento del usuario originario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Nombre_Destinatario) || string.IsNullOrEmpty(archivoDto.Nombre_Destinatario))
                    {
                        return Results.BadRequest("El nombre del usuario destinatario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_1_Destinatario) || string.IsNullOrEmpty(archivoDto.Apellido_1_Destinatario))
                    {
                        return Results.BadRequest("El primer apellido del usuario destinatario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Apellido_2_Destinatario) || string.IsNullOrEmpty(archivoDto.Apellido_2_Destinatario))
                    {
                        return Results.BadRequest("El segundo apellido del usuario destinatario(a) no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Correo_Electronico_Destinatario) || string.IsNullOrEmpty(archivoDto.Correo_Electronico_Destinatario))
                    {
                        return Results.BadRequest("El correo electrónico del usuario destinatario(a) no puede ser nulo.");
                    }

                    bool respuestaValidacion_Destinatario = ValidarCorreo(archivoDto.Correo_Electronico_Destinatario);
                    if (respuestaValidacion_Destinatario == false)
                    {
                        return Results.BadRequest("El correo electrónico del usuario destinatario(a) que fue proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Departamento_Destinatario) || string.IsNullOrEmpty(archivoDto.Departamento_Destinatario))
                    {
                        return Results.BadRequest("El departamento del usuario destinatario(a) no puede ser nulo.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario y el empleado(a) en cuestión. Por lo que se -
                     * mandaria un mensaje de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    Task<UsuarioEntitie?> UsuarioNuevo = usuarioRepo.ObtenerUsuario_PorCorreo(archivoDto.Correo_Electronico_Destinatario)!;
                    
                    if (Usuario.Result == null || UsuarioNuevo.Result == null)
                    {
                        return Results.BadRequest("No existe ese usuario(a) en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;
                    int idNuevoUsuario = UsuarioNuevo!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    Task<EmpleadoEntitie?> EmpleadoNuevo = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idNuevoUsuario)!;
                    
                    if (Empleado.Result == null || EmpleadoNuevo.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;
                    int idEmpleadoNuevo = EmpleadoNuevo!.Result!.Id;


                    /* Aqui lo que se hace es actualizar el registro de archivo. Ahora, -
                     * si en la variable: "resultadoArchivo" resulta ser nulo o falso, -
                     * entonces quiere decir que ocurrio un error o que no existe ese -
                     * registro de archivo. Por lo tanto, se mandaria un mensaje de -
                     * error respectivamente. */
                    var resultadoArchivo = await archivoRepo.ActualizarRegistrosArchivos(archivoDto, idEmpleado, idEmpleadoNuevo);
                    if (resultadoArchivo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización del registro de archivo.");
                    }

                    if (resultadoArchivo == false)
                    {
                        return Results.BadRequest("No existe ese registro de archivo en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar Archivo";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar un registro de un archivo que " +
                    "esta asociado a un empleado destinatario(a). " + "El cual es el siguiente: " + UsuarioNuevo!.Result!.Nombre + " " + UsuarioNuevo!.Result!.Apellido_1 +
                    " " + UsuarioNuevo!.Result!.Apellido_2 + ", con cédula: " + UsuarioNuevo!.Result!.Cedula + " y correo electrónico: " + UsuarioNuevo!.Result!.Correo_Electronico 
                    + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoArchivo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar el estado de un registro de archivo:
            app.MapPut("/api/actualizarEstadoArchivo", async (string estadoArchivo, string fechaSubida, string correoEmpleado, 
                string tokenAcceso, IEmpleadoRepository empleadoRepo, IJwtRepository jwtRepo, IRegistroArchivoRepository archivoRepo, 
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar los datos del registro de archivo -
                     * que se está suministrando. Si resulta que da algún tipo de error, -
                     * entonces se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(estadoArchivo) || string.IsNullOrEmpty(estadoArchivo))
                    {
                        return Results.BadRequest("El estado del archivo no puede ser nulo.");
                    }


                    if (string.IsNullOrWhiteSpace(fechaSubida) || string.IsNullOrEmpty(fechaSubida))
                    {
                        return Results.BadRequest("La fecha de subida no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(correoEmpleado) || string.IsNullOrEmpty(correoEmpleado))
                    {
                        return Results.BadRequest("El correo electrónico no puede ser nulo.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoEmpleado);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario y el empleado(a) en cuestión. Por lo que se -
                     * mandaria un mensaje de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese usuario(a) en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es actualizar el estado de ese registro de archivo. -
                     * Ahora, si en la variable: "resultadoArchivo" resulta ser nulo o falso, -
                     * entonces quiere decir que ocurrio un error o que no existe ese registro -
                     * de archivo. Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoArchivo = await archivoRepo.ActualizarEstadoRegistroArchivo(estadoArchivo, fechaSubida, idEmpleado);
                    if (resultadoArchivo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la actualización del registro de archivo.");
                    }

                    if (resultadoArchivo == false)
                    {
                        return Results.BadRequest("No existe ese registro de archivo en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Actualizar Estado Archivo";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para actualizar un estado de un archivo que " +
                    "esta asociado a un empleado destinatario(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + 
                    " " + Usuario!.Result!.Apellido_2 + ", con cédula: " + Usuario!.Result!.Cedula + " y correo electrónico: " + Usuario!.Result!.Correo_Electronico 
                    + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoArchivo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para compartir los registros de archivos:
            app.MapPut("/api/compartirArchivo", async (RegistroArchivoCreateDto archivoDto, string tokenAcceso,
                IJwtRepository jwtRepo, IRegistroArchivoRepository archivoRepo, IUsuarioRepository usuarioRepo,
                IEmpleadoRepository empleadoRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar los datos del registro de archivo que -
                     * se está suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(archivoDto.Correo_Electronico_Originario) || string.IsNullOrEmpty(archivoDto.Correo_Electronico_Originario))
                    {
                        return Results.BadRequest("El correo electrónico del usuario originario(a) no puede ser nulo.");
                    }

                    bool respuestaValidacion_Originario = ValidarCorreo(archivoDto.Correo_Electronico_Originario);
                    if (respuestaValidacion_Originario == false)
                    {
                        return Results.BadRequest("El correo electrónico del usuario originario(a) que fue proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Fecha_Subida) || string.IsNullOrEmpty(archivoDto.Fecha_Subida))
                    {
                        return Results.BadRequest("La fecha de subida no puede ser nula.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Correo_Electronico_Destinatario) || string.IsNullOrEmpty(archivoDto.Correo_Electronico_Destinatario))
                    {
                        return Results.BadRequest("El correo electrónico del usuario destinatario(a) no puede ser nulo.");
                    }

                    bool respuestaValidacion_Destinatario = ValidarCorreo(archivoDto.Correo_Electronico_Destinatario);
                    if (respuestaValidacion_Destinatario == false)
                    {
                        return Results.BadRequest("El correo electrónico del usuario destinatario(a) que fue proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(archivoDto.Estado_Archivo) || string.IsNullOrEmpty(archivoDto.Estado_Archivo))
                    {
                        return Results.BadRequest("El estado del archivo no puede ser nulo.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario y el empleado(a) en cuestión. Por lo que se -
                     * mandaria un mensaje de error como respuesta. */
                    Task<UsuarioEntitie?> UsuarioOriginario = usuarioRepo.ObtenerUsuario_PorCorreo(archivoDto.Correo_Electronico_Originario)!;
                    if (UsuarioOriginario.Result == null)
                    {
                        return Results.BadRequest("No existe ese usuario originario(a) en el sistema.");
                    }

                    Task<UsuarioEntitie?> UsuarioDestinatario = usuarioRepo.ObtenerUsuario_PorCorreo(archivoDto.Correo_Electronico_Destinatario)!;
                    if (UsuarioDestinatario.Result == null)
                    {
                        return Results.BadRequest("No existe ese usuario destinatario(a) en el sistema.");
                    }

                    int idUsuarioDestinatario = UsuarioDestinatario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuarioDestinatario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es compartir el registro de archivo. Ahora, si en -
                     * la variable: "resultadoArchivo" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese registro de -
                     * archivo. Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoArchivo = await archivoRepo.CompartirRegistrosArchivos(archivoDto, idEmpleado);
                    if (resultadoArchivo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante el proceso para compartir el registro de archivo.");
                    }

                    if (resultadoArchivo == false)
                    {
                        return Results.BadRequest("No existe ese registro de archivo en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Compartir Archivo";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para compartir un registro de un archivo que " +
                    "esta asociado a un empleado destinatario(a). " + "El cual es el siguiente: " + UsuarioDestinatario!.Result!.Nombre + " " + 
                    UsuarioDestinatario!.Result!.Apellido_1 + " " + UsuarioDestinatario!.Result!.Apellido_2 + ", con cédula: " + 
                    UsuarioDestinatario!.Result!.Cedula + " y correo electrónico: " + UsuarioDestinatario!.Result!.Correo_Electronico + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoArchivo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();



            //                     |=============| DELETE |=============|

            //Ruta (tipo: DELETE) del API que sirve para eliminar una pregunta y respuesta:
            app.MapDelete("/api/eliminarFAQ", async (string preguntaFAQ, IFAQRepository preguntasYrespuestasRepo,
                string tokenAcceso, IJwtRepository jwtRepo, IUsuarioRepository usuarioRepo, 
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar la pregunta y respuesta que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces se -
                     * mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(preguntaFAQ) || string.IsNullOrEmpty(preguntaFAQ))
                    {
                        return Results.BadRequest("La pregunta no puede ser nula.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    string correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;

                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;


                    /* Aqui lo que se hace es eliminar una pregunta y respuesta. Ahora, si en -
                     * la variable: "resultadoFAQ" resulta ser nulo o falso, entonces quiere -
                     * decir que ocurrio un error o que no existe esa pregunta y respuesta. -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoFAQ = await preguntasYrespuestasRepo.EliminarFAQ(preguntaFAQ, idUsuario);
                    if (resultadoFAQ == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la eliminación del FAQ que fue seleccionado.");
                    }

                    if (resultadoFAQ == false)
                    {
                        return Results.BadRequest("No existe ese FAQ en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el usuario(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Eliminar FAQ";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para poder eliminar una pregunta y respuesta, " +
                    "el cual es el siguiente: " + preguntaFAQ + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoFAQ);
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: DELETE) del API que sirve para eliminar a un empleado(a):
            app.MapDelete("/api/eliminarEmpleado", async (string correoEmpleado, string tokenAcceso,
                IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo,
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador o un administrador(a), y si no lo tiene, entonces -
                 * no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aqui lo que se hace es validar el correo electrónico que se está -
                     * suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correoEmpleado) || string.IsNullOrEmpty(correoEmpleado))
                    {
                        return Results.BadRequest("El correo electrónico no puede ser nulo.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoEmpleado);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;


                    /* Aqui lo que se hace es eliminar un empleado(a). Ahora, si en -
                     * la variable: "resultadoEmpleado" resulta ser nulo o falso, -
                     * entonces quiere decir que ocurrio un error o que no existe -
                     * ese empleado(a). Por lo tanto, se mandaria un mensaje de -
                     * error respectivamente. */
                    var resultadoEmpleado = await empleadoRepo.EliminarEmpleado(idUsuario);
                    if (resultadoEmpleado == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la eliminación del empleado(a) que fue seleccionado.");
                    }

                    if (resultadoEmpleado == false)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es eliminar el usuario que esta asociado a ese -
                     * empleado(a). Ahora, si en la variable: "resultadoUsuario" resulta -
                     * ser nulo o falso, entonces quiere decir que ocurrio un error o que -
                     * no existe ese usuario(a). Por lo tanto, se mandaria un mensaje de -
                     * error respectivamente. */
                    var resultadoUsuario = await usuarioRepo.EliminarUsuario(idUsuario);
                    if (resultadoUsuario == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la eliminación del empleado(a) que fue seleccionado.");
                    }

                    if (resultadoUsuario == false)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Eliminar Empleado";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para eliminar un empleado(a), " +
                    "el cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " + Usuario!.Result!.Apellido_2 + 
                    ", con cédula: " + Usuario!.Result!.Cedula + " y correo electrónico: " + Usuario!.Result!.Correo_Electronico + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoEmpleado);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: DELETE) del API que sirve para eliminar un permiso de tiempo:
            app.MapDelete("/api/eliminarPermisosTiempo", async (string cedulaUsuario, string tokenAcceso, IJwtRepository jwtRepo, 
                IPermisoTiempoRepository tiempoRepo, IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo, 
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador(a), y si no lo tiene, entonces no lo dejara -
                 * pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1)
                {
                    /* Aqui lo que se hace es validar la cédula que se está suministrando. -
                     * Si resulta que da algún tipo de error, entonces se mandaria su -
                     * respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario) || string.IsNullOrEmpty(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula no puede ser nula.");
                    }

                    if (cedulaUsuario.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicha cédula.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe esa -
                     * cédula dentro de la base de datos, por ende, también no existiria el -
                     * usuario y el empleado(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es eliminar un permiso de tiempo. Ahora, si -
                     * en la variable: "resultadoPermisosTiempo" resulta ser nulo o falso, -
                     * entonces quiere decir que ocurrio un error o que no existe ese permiso -
                     * de tiempo. Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoPermisosTiempo = await tiempoRepo.EliminarPermisosTiempo(idEmpleado);
                    if (resultadoPermisosTiempo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la eliminación del permiso de tiempo que fue seleccionado.");
                    }

                    if (resultadoPermisosTiempo == false)
                    {
                        return Results.BadRequest("No existe ese permiso de tiempo en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Eliminar Permisos Tiempo";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para eliminar un permiso de tiempo que esta " +
                    "asociado a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " + 
                    Usuario!.Result!.Apellido_2 + ", con cédula: " + Usuario!.Result!.Cedula + " respectivamente.";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoPermisosTiempo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: DELETE) del API que sirve para eliminar un salario:
            app.MapDelete("/api/eliminarSalarios", async (string cedulaUsuario, string tokenAcceso, IJwtRepository jwtRepo, 
                ISalarioRepository salarioRepo, IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo,
                IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador(a), y si no lo tiene, entonces no lo dejara -
                 * pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1)
                {
                    /* Aqui lo que se hace es validar la cédula que se está suministrando. -
                     * Si resulta que da algún tipo de error, entonces se mandaria su -
                     * respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario) || string.IsNullOrEmpty(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula no puede ser nula.");
                    }

                    if (cedulaUsuario.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula no cumple con los requisitos establecidos.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicha cédula.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe esa -
                     * cédula dentro de la base de datos, por ende, también no existiria el -
                     * usuario y empleado(a) en cuestión. Por lo que se mandaria un mensaje -
                     * de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es eliminar un salario. Ahora, si en la -
                     * variable: "resultadoSalario" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese salario. -
                     * Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoSalario = await salarioRepo.EliminarSalarios(idEmpleado);
                    if (resultadoSalario == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la eliminación del salario que fue seleccionado.");
                    }

                    if (resultadoSalario == false)
                    {
                        return Results.BadRequest("No existe ese salario en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Eliminar Salarios";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para eliminar un salario que esta " +
                    "asociado a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " +
                    Usuario!.Result!.Apellido_2 + ", con cédula: " + Usuario!.Result!.Cedula + " respectivamente.";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoSalario);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: DELETE) del API que sirve para eliminar un registro de archivo:
            app.MapDelete("/api/eliminarArchivo", async (string correoEmpleado, string fechaSubida, string tokenAcceso, 
                IJwtRepository jwtRepo, IRegistroArchivoRepository archivoRepo, IEmpleadoRepository empleadoRepo, 
                IUsuarioRepository usuarioRepo, IHistoricoRepository historicoRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado(a). */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario(a), -
                 * el rol es de un moderador, un administrador o un empleado(a), y si no lo -
                 * tiene, entonces no lo dejara pasar respectivamente. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se hace es validar los datos del registro de archivo que -
                     * se está suministrando. Si resulta que da algún tipo de error, entonces -
                     * se mandaria su respectivo mensaje de error. */
                    if (string.IsNullOrWhiteSpace(correoEmpleado) || string.IsNullOrEmpty(correoEmpleado))
                    {
                        return Results.BadRequest("El correo electrónico no puede ser nulo.");
                    }

                    bool respuestaValidacion = ValidarCorreo(correoEmpleado);
                    if (respuestaValidacion == false)
                    {
                        return Results.BadRequest("El correo electrónico proporcionado no es valido.");
                    }


                    if (string.IsNullOrWhiteSpace(fechaSubida) || string.IsNullOrEmpty(fechaSubida))
                    {
                        return Results.BadRequest("La fecha de subida no puede ser nula.");
                    }


                    /* Aqui lo que se hace es traer toda la información del usuario(a) al cual -
                     * le pertenece dicho correo eletrónico.
                     * 
                     * Ahora si resulta que da un nulo entonces quiere decir que no existe ese -
                     * correo electrónico dentro de la base de datos, por ende, también no -
                     * existiria el usuario y empleado(a) en cuestión. Por lo que se -
                     * mandaria un mensaje de error como respuesta. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;


                    /* Aqui lo que se hace es eliminar un registro de archivo. Ahora, si en -
                     * la variable: "resultadoArchivo" resulta ser nulo o falso, entonces -
                     * quiere decir que ocurrio un error o que no existe ese registro de -
                     * archivo. Por lo tanto, se mandaria un mensaje de error respectivamente. */
                    var resultadoArchivo = await archivoRepo.EliminarRegistrosArchivos(fechaSubida, idEmpleado);
                    if (resultadoArchivo == null)
                    {
                        return Results.BadRequest("Ocurrio un error durante la eliminación del registro de archivo que fue seleccionado.");
                    }

                    if (resultadoArchivo == false)
                    {
                        return Results.BadRequest("No existe ese registro de archivo en el sistema.");
                    }


                    /* Aqui lo que se hace es registrar la acción de este endpoint, esto debido a -
                     * que se tomara nota de lo que hizo el empleado(a) para tenerlo como un registro -
                     * historico. Lo que ayudaria en temas de auditoria respectivamente. */
                    string nombre = respuestaValidarToken.FindFirst("nombre")!.Value;
                    string correo = respuestaValidarToken.FindFirst("correo")!.Value;
                    string accion = "Eliminar Archivo";
                    string descripcion = "El usuario(a): " + nombre + " realizo una solicitud al API para eliminar un registro de un archivo que " +
                    "esta asociado a un empleado(a). " + "El cual es el siguiente: " + Usuario!.Result!.Nombre + " " + Usuario!.Result!.Apellido_1 + " " +
                    Usuario!.Result!.Apellido_2 + ", con cédula: " + Usuario!.Result!.Cedula + " y correo electrónico: " + Usuario!.Result!.Correo_Electronico 
                    + ".";

                    historicoRepo.CrearHistorico(nombre, correo, accion, descripcion);

                    return Results.Ok(resultadoArchivo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Comando para ejecutar el proyecto:
            app.Run();
        }


        /* Método que sirve para poder validar si el correo electrónico proporcionado por el usuario(a) -
         * esta correcto o no. */
        public static bool ValidarCorreo(string correoParametrizado)
        {
            try
            {
                /* Aquí como tal lo que se esta haciendo es obtener el @ y los caracteres que están -
                 * después de dicho @, el cual seria el dominio como gmail.com por dar un ejemplo. -
                 * Y una vez que se obtiene el resultado, simplemente se llevaria a la función: -
                 * MapearDominio respectivamente. */
                correoParametrizado = Regex.Replace(correoParametrizado, @"(@)(.+)$", MapearDominio, 
                    RegexOptions.None, TimeSpan.FromMilliseconds(200));


               /* Lo que se hace aqui es tomar el resultado que se obtuvo en la validación, y -
                * luego de eso traducir los caracteres del dominio hacia al estandar US-ASCII. -
                * Esto debido a que los caracteres estaban en un tipo: Unicode, y ese tipo de -
                * caracteres no los puede leer el US-ASCII. 
                * 
                * Una vez hecho eso, simplemente se concatenaria el dominio traducido con el @ -
                * respectivamente. Eso si, tanto en la validación como en esta función, se coloco -
                * un try catch para detectar si el dominio no es valido o si excede el tiempo que -
                * se establecio, esto para evitar posibles errores o incluso ataques de DNS. */
                string MapearDominio(Match resultadoCoincidencia)
                {
                    var idn = new IdnMapping();

                    string nombreDominio = idn.GetAscii(resultadoCoincidencia.Groups[2].Value);

                    return resultadoCoincidencia.Groups[1].Value + nombreDominio;
                }
            }
            catch (RegexMatchTimeoutException error)
            {
                return false;
            }
            catch (ArgumentException error)
            {
                return false;
            }

            try
            {
                /* Si paso lo anterior, entonces se validaria si ese correo electrónico -
                 * esta correcto, y si no entonces devolveria un falso respectivamente. -
                 * Además en esta validación se incluye las mayúsculas y minúsculas, de -
                 * ahí el porque se coloco: RegexOptions.IgnoreCase.
                 *
                 * Asimismo, para evitar ataques de DNS, también se coloco un tiempo para -
                 * que se pueda realizar bien la validación. */
                return Regex.IsMatch(correoParametrizado, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException error)
            {
                return false;
            }
        }
    }
}
