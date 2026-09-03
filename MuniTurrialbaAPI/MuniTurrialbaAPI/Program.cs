
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;
using MuniTurrialbaAPI.Repositories;
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

            // Add services to the container.
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
                //Esto es para agregar un titulo al API cuando se ejecute:
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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            /* Aqui se agrega esto para que el API pueda -
             * utilizar la autenticación. 
             *
             * NOTA: Si se pone dicho comando después del: -
             * "app.UseAuthorization();" o si no se pone -
             * directamente, entonces el API daria un error. */
            app.UseAuthentication();
            app.UseAuthorization();


            //           |=============| RUTAS DEL API |=============|

            //           |=============| GET |=============|
            //Ruta (tipo: GET) del API que sirve para traer todos los usuarios:
            app.MapGet("/api/usuarios", async (IUsuarioRepository usuarioRepo, 
                string tokenAcceso, IJwtRepository jwtRepo) => 
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3) 
                {
                    /* Aquí lo que se indica es que llama al método: "ObtenerUsuarios()" -
                     * para traer a todos los usuarios que hay en la BD. 
                     * 
                     * Ahora, si en la variable: "usuarios" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    var usuarios = await usuarioRepo.ObtenerUsuarios();
                    if (usuarios is null)
                    {
                        return Results.NotFound("No se pudieron obtener los usuarios.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todos los usuarios -
                     * respectivamente. Además de indicar el código, que seria un código #200. */
                    return Results.Ok(usuarios);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer a todos los usuarios que iniciaron sesión:
            app.MapGet("/api/iniciosSesion", async (IInicioSesionRepository sesionRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aquí lo que se indica es que llama al método: "ObtenerRegistros_InicioSesion()" -
                     * para traer a todos los usuarios que hay en la BD. 
                     * 
                     * Ahora, si en la variable: "iniciosSesion" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    var iniciosSesion = await sesionRepo.ObtenerRegistros_InicioSesion();
                    if (iniciosSesion is null)
                    {
                        return Results.NotFound("No se pudieron obtener los inicios de sesion.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todos los iniciosSesion -
                     * respectivamente. Además de indicar el código, que seria un código #200. */
                    return Results.Ok(iniciosSesion);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer todos los empleados:
            app.MapGet("/api/empleados", async (IEmpleadoRepository empleadoRepo,
                string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aquí lo que se indica es que llama al método: "ObtenerEmpleados()" -
                     * para traer a todos los usuarios que hay en la BD. 
                     * 
                     * Ahora, si en la variable: "empleados" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    var empleados = await empleadoRepo.ObtenerEmpleados();
                    if (empleados is null)
                    {
                        return Results.NotFound("No se pudieron obtener los empleados.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todos los empleados -
                     * respectivamente. Además de indicar el código, que seria un código #200. */
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
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aquí lo que se indica es que llama al método: "ObtenerEmpleados()" -
                     * para traer a todos los usuarios que hay en la BD. 
                     * 
                     * Ahora, si en la variable: "empleados" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correo)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo en el sistema.");
                    }
                    int idUsuario = Usuario!.Result!.Id;

                    var permisos = await permisoRepo.ObtenerPermisosUsuario(idUsuario);
                    if (permisos is null)
                    {
                        return Results.NotFound("No se pudieron obtener los permisos.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todos los empleados -
                     * respectivamente. Además de indicar el código, que seria un código #200. */
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
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aquí lo que se indica es que llama al método: "ObtenerEmpleados()" -
                     * para traer a todos los usuarios que hay en la BD. 
                     * 
                     * Ahora, si en la variable: "empleados" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    var permisosTiempo = await tiempoRepo.ObtenerPermisosTiempo();
                    if (permisosTiempo is null)
                    {
                        return Results.NotFound("No se pudieron obtener los permisos de tiempo.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todos los empleados -
                     * respectivamente. Además de indicar el código, que seria un código #200. */
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
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Aquí lo que se indica es que llama al método: "ObtenerEmpleados()" -
                     * para traer a todos los usuarios que hay en la BD. 
                     * 
                     * Ahora, si en la variable: "empleados" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    var salarios = await salarioRepo.ObtenerSalarios();
                    if (salarios is null)
                    {
                        return Results.NotFound("No se pudieron obtener los salarios.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todos los empleados -
                     * respectivamente. Además de indicar el código, que seria un código #200. */
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
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {

                    /* Aquí lo que se indica es que llama al método: "ObtenerFAQs()" -
                     * para traer a todas las preguntas y respuestas que hay en la BD. 
                     * 
                     * Ahora, si en la variable: "fAQs" es nulo, entonces quiere decir -
                     * que no hay nada en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    var fAQs = await preguntasYrespuestasRepo.ObtenerFAQs();
                    if (fAQs is null)
                    {
                        return Results.NotFound("No se pudieron obtener las preguntas y respuestas.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todas las preguntas -
                     * y respuestas respectivamente. Además de indicar el código, que seria un -
                     * código #200. */
                    return Results.Ok(fAQs);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para traer un usuario por medio de un correo:
            app.MapGet("/api/usuario/{correo:required}", async (string correo, string tokenAcceso,
                IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aquí lo que se indica es que llama al método: "ObtenerUsuario_PorCorreo()" -
                     * para traer a un usuario que existe en la BD, esto por medio del correo.
                     * 
                     * Ahora, si en la variable: "usuario" es nulo, entonces quiere decir que no -
                     * hay nada en la BD. Por lo tanto se manda un error al usuario respectivamente. */
                    var usuario = await usuarioRepo.ObtenerUsuario_PorCorreo(correo);
                    if (usuario is null)
                    {
                        return Results.NotFound("No se pudo obtener el usuario.");
                    }


                    /* Si no hubo ningún problema, entonces se mostrarian todos los usuarios -
                     * respectivamente. Además de indicar el código, que seria un código #200. */
                    return Results.Ok(usuario);
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: GET) del API que sirve para validar el código proporcionado:
            app.MapGet("/api/validarcodigo/{codigo:required}", (string codigo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Es para validar si el código tiene datos, si es nulo o esta en blanco -
                 * entonces el API tendria que dar un mensaje indicando que el código es -
                 * necesario. */
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    return Results.BadRequest("El código es necesario.");
                }

                /* Aquí lo que se indica es que llama al método: "VerificarCodigo()" -
                 * para así poder verificar si el código proporcionado es el mismo -
                 * que se envio por el correo electrónico.
                 * 
                 * Ahora, si en la variable: "respuestaRecuperacion" es falso, entonces -
                 * quiere decir que el código no es el correcto. Por lo tanto se manda -
                 * un error al usuario respectivamente. */
                var respuestaRecuperacion = usuarioRepo.VerificarCodigo(codigo);
                if (respuestaRecuperacion != true)
                {
                    return Results.BadRequest("El código que fue ingresado esta incorrecto.");
                }


                /* Si no hubo ningún problema, entonces indicaria que el código proporcionado -
                 * coincide con el que se le envio al correo electrónico respectivamente. Además -
                 * de indicar el código: #200. */
                return Results.Ok(respuestaRecuperacion);
            });


            //Ruta (tipo: GET) del API que sirve obtener el código QR con los datos del usuario:
            app.MapGet("/api/obtenerQR", (string nombre, string apellidos, string correo, string tokenAcceso,
                IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Es para validar si el nombre tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el nombre es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        return Results.BadRequest("El nombre es necesario.");
                    }


                    /* Es para validar si los apellidos tienen datos, si es nulo o esta en -
                     * blanco entonces el API tendria que dar un mensaje indicando que los -
                     * apellidos son necesarios. */
                    if (string.IsNullOrWhiteSpace(apellidos))
                    {
                        return Results.BadRequest("Los apellidos son necesarios.");
                    }


                    /* Es para validar si el correo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el correo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(correo))
                    {
                        return Results.BadRequest("El correo es necesario.");
                    }


                    /* Aquí lo que se indica es que llama al método: "CrearCodigoQR()" -
                     * para crear el código QR. 
                     * 
                     * Ahora, si en la variable: "codigoQR" es nulo, entonces quiere decir -
                     * que no se pudo crear dicho código. Por lo tanto se manda un error al -
                     * usuario respectivamente. */
                    var codigoQR = usuarioRepo.CrearCodigoQR(nombre, apellidos, correo);
                    if (codigoQR.ToString() == null)
                    {
                        return Results.BadRequest("No se pudo obtener el código QR.");
                    }


                    /* Aquí lo que se indica es que si en la variable: "codigoQR" es falso, -
                     * entonces quiere decir que ese correo no existe en la BD. Por lo tanto -
                     * se manda un error al usuario respectivamente. */
                    if (codigoQR.ToString() == "false")
                    {
                        return Results.BadRequest("El correo electrónico no existe en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces se crearia el código QR del usuario -
                     * respectivamente. Además de indicar un código #200. */
                    return Results.Ok(codigoQR);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();




            //                     |=============| POST |=============|

            //Ruta (tipo: POST) del API que sirve para que el usuario pueda iniciar sesión:
            app.MapPost("/api/iniciarsesion", (ExtensionUsuarioCreateDto usuarioDto, IUsuarioRepository 
                usuarioRepo, IInicioSesionRepository sesionRepo, IJwtRepository jwtRepo, IPermisoRepository 
                permisoRepo) => 
            {
                /* Es para validar si el correo tiene datos, si es nulo o esta en blanco -
                 * entonces el API tendria que dar un mensaje indicando que el correo es -
                 * necesario. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico))
                {
                    return Results.BadRequest("El correo es necesario.");
                }


                /* Es para validar si la contraseña tiene datos, si es nulo o esta en blanco -
                 * o incluso si no cumple con la cantidad minima de digitos (que es 12) - 
                 * entonces el API tendria que dar un mensaje indicando que la contraseña - 
                 * es necesaria. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || usuarioDto.Contraseña.Trim().Length < 12)
                {
                    return Results.BadRequest("La contraseña es necesaria.");
                }


                /* Aquí lo que se indica es que llama al método: "VerificarUsuario()" -
                 * para verificar si el correo y la contraseña de ese usuario son -
                 * los mismos que hay en la BD.
                 * 
                 * Ahora, si en la variable: "respuestaVerificacion_Usuario" es falso, -
                 * entonces quiere decir que ese usuario no existe en la BD. Por lo -
                 * tanto se manda un error al usuario respectivamente. */
                var respuestaVerificacion_Usuario = usuarioRepo.VerificarUsuario(usuarioDto.Correo_Electronico, usuarioDto.Contraseña);
                if (respuestaVerificacion_Usuario == null)
                {
                    return Results.BadRequest("El usuario no existe en el sistema.");
                }


                /* Aquí lo que se indica es que llama al método: "ObtenerPermisosUsuario()" -
                 * para obtener todos los permisos que tiene ese usuario que esta intentando -
                 * iniciar sesión.
                 * 
                 * Ahora, si en la variable: "respuestaPermisos_Usuario" es nulo, entonces -
                 * quiere decir que no hay nada en la BD. Por lo tanto se manda un error al -
                 * usuario respectivamente. */
                var respuestaPermisos_Usuario = permisoRepo.ObtenerPermisosUsuario(respuestaVerificacion_Usuario.Result!.Id);
                if (respuestaPermisos_Usuario == null)
                {
                    return Results.BadRequest("El usuario no contiene permisos en el sistema.");
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

                    new Claim("permiso_Leer", respuestaPermisos_Usuario.Result!.Leer.ToString()),
                    new Claim("permiso_Crear", respuestaPermisos_Usuario.Result!.Crear.ToString()),
                    new Claim("permiso_Actualizar", respuestaPermisos_Usuario.Result!.Actualizar.ToString()),
                    new Claim("permiso_Eliminar", respuestaPermisos_Usuario.Result!.Eliminar.ToString())
                };

                InicioSesionCreateDto inicioSesionCreateDto = new InicioSesionCreateDto();

                inicioSesionCreateDto.Fecha_Inicio_Sesion = DateTime.Now;
                inicioSesionCreateDto.Hora = TimeOnly.FromDateTime(DateTime.Now);
                inicioSesionCreateDto.Ultima_Conexion = DateTime.Now;

                var respuestaInicioSesion = sesionRepo.CrearRegistroInicioSesion(inicioSesionCreateDto, respuestaVerificacion_Usuario.Result!.Id);
                 if (respuestaInicioSesion == null)
                {
                    return Results.BadRequest("No se pudo continuar con el inicio de sesión.");
                }


                /* Y una vez colocado todo lo necesario en los claims, entonces se pasarian -
                 * al método: crearTokenJWT(), para que ahora así se pueda crear el token JWT -
                 * del usuario, y luego se pueda mandar en el result respectivamente. */
                var respuestaToken = jwtRepo.crearTokenJWT(claims_TokenGenerar);                              
                
                return Results.Created($"/api/iniciarsesion/{respuestaToken}", 
                    new { TokenAcceso = respuestaToken });
            });


            //Ruta (tipo: POST) del API que sirve para crear un usuario:
            app.MapPost("/api/crearusuarios", async (UsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo, bool tipo) =>
            {
                    /* Es para validar si el nombre tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el nombre es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Nombre))
                    {
                        return Results.BadRequest("El nombre es necesario.");
                    }


                    /* Es para validar si el primer apellido tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el -
                     * primer apellido es necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_1))
                    {
                        return Results.BadRequest("El primer apellido es necesario.");
                    }


                    /* Es para validar si el segundo apellido tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el - 
                     * segundo apellido es necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_2))
                    {
                        return Results.BadRequest("El segundo apellido es necesario.");
                    }


                    /* Es para validar si la edad tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la edad es -
                     * necesaria.
                     * 
                     * De igual manera haria lo mismo si detecta que la edad lo dejaron en cero, -
                     * o si ponen una edad mayor a 99 (lo que significaria una edad de 3 digitos) -
                     * respectivamente. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Edad.ToString()) || usuarioDto.Edad == 0 || usuarioDto.Edad > 99)
                    {
                        return Results.BadRequest("La edad es necesaria.");
                    }


                    /* Es para validar si la cédula tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la cédula es -
                     * necesaria.
                     * 
                     * De igual manera haria lo mismo si detecta que la cédula es mayor a 12 digitos, -
                     * ya que en Costa Rica hay un tamaño definido para la cédula respectivamente. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Cedula) || usuarioDto.Cedula.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }


                    /* Es para validar si el correo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el correo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico))
                    {
                        return Results.BadRequest("El correo es necesario.");
                    }


                    /* Es para validar si la contraseña tiene datos, si es nulo o esta en blanco -
                     * o incluso si no cumple con la cantidad minima de digitos (que es 12) - 
                     * entonces el API tendria que dar un mensaje indicando que la contraseña - 
                     * es necesaria. */
                     if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || usuarioDto.Contraseña.Trim().Length < 12)
                     {
                        return Results.BadRequest("La contraseña es necesaria.");
                     }


                    /* Es para validar si el rol tiene datos, si es nulo o esta en blanco
                     * entonces el API tendria que dar un mensaje indicando que el rol es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Id_Rol.ToString()))
                    {
                        return Results.BadRequest("El rol es necesario.");
                    }

                    /* Es para validar si el rol tiene datos, si es nulo o esta en blanco
                     * entonces el API tendria que dar un mensaje indicando que el rol es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tipo.ToString()))
                    {
                        return Results.BadRequest("Error.");
                    }


                    /* Aquí lo que se indica es que llama al método: "CrearUsuario()" -
                     * para poder crear un usuario con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoUsuario" es nulo, entonces -
                     * quiere decir que no se pudo crear ese usuario en la BD. Por -
                     * lo tanto se manda un error al usuario respectivamente. */
                    var nuevoUsuario = await usuarioRepo.CrearUsuario(usuarioDto, tipo);
                    if (nuevoUsuario == null)
                    {
                        return Results.BadRequest("No se pudo crear el usuario.");
                    }

                    /* También se valida si en la variable: "nuevoUsuario" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un usuario con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (nuevoUsuario == false)
                    {
                        return Results.BadRequest("Ya existe una cuenta con ese correo o cédula en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Created($"/api/crearusuarios/{nuevoUsuario}", 
                        new { Respuesta = nuevoUsuario });
            });


            //Ruta (tipo: POST) del API que sirve para crear un usuario:
            app.MapPost("/api/crearusuario", async (UsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo, bool tipo, string tokenAcceso, 
                IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Es para validar si el nombre tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el nombre es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Nombre))
                    {
                        return Results.BadRequest("El nombre es necesario.");
                    }


                    /* Es para validar si el primer apellido tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el -
                     * primer apellido es necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_1))
                    {
                        return Results.BadRequest("El primer apellido es necesario.");
                    }


                    /* Es para validar si el segundo apellido tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el - 
                     * segundo apellido es necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_2))
                    {
                        return Results.BadRequest("El segundo apellido es necesario.");
                    }


                    /* Es para validar si la edad tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la edad es -
                     * necesaria.
                     * 
                     * De igual manera haria lo mismo si detecta que la edad lo dejaron en cero, -
                     * o si ponen una edad mayor a 99 (lo que significaria una edad de 3 digitos) -
                     * respectivamente. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Edad.ToString()) || usuarioDto.Edad == 0 || usuarioDto.Edad > 99)
                    {
                        return Results.BadRequest("La edad es necesaria.");
                    }


                    /* Es para validar si la cédula tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la cédula es -
                     * necesaria.
                     * 
                     * De igual manera haria lo mismo si detecta que la cédula es mayor a 12 digitos, -
                     * ya que en Costa Rica hay un tamaño definido para la cédula respectivamente. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Cedula) || usuarioDto.Cedula.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }


                    /* Es para validar si el correo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el correo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico))
                    {
                        return Results.BadRequest("El correo es necesario.");
                    }


                    /* Es para validar si la contraseña tiene datos, si es nulo o esta en blanco -
                     * o incluso si no cumple con la cantidad minima de digitos (que es 12) - 
                     * entonces el API tendria que dar un mensaje indicando que la contraseña - 
                     * es necesaria. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || usuarioDto.Contraseña.Trim().Length < 12)
                    {
                        return Results.BadRequest("La contraseña es necesaria.");
                    }


                    /* Es para validar si el rol tiene datos, si es nulo o esta en blanco
                     * entonces el API tendria que dar un mensaje indicando que el rol es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Id_Rol.ToString()))
                    {
                        return Results.BadRequest("El rol es necesario.");
                    }

                    /* Es para validar si el rol tiene datos, si es nulo o esta en blanco
                     * entonces el API tendria que dar un mensaje indicando que el rol es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tipo.ToString()))
                    {
                        return Results.BadRequest("Error.");
                    }


                    /* Aquí lo que se indica es que llama al método: "CrearUsuario()" -
                     * para poder crear un usuario con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoUsuario" es nulo, entonces -
                     * quiere decir que no se pudo crear ese usuario en la BD. Por -
                     * lo tanto se manda un error al usuario respectivamente. */
                    var nuevoUsuario = await usuarioRepo.CrearUsuario(usuarioDto, tipo);
                    if (nuevoUsuario == null)
                    {
                        return Results.BadRequest("No se pudo crear el usuario.");
                    }

                    /* También se valida si en la variable: "nuevoUsuario" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un usuario con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (nuevoUsuario == false)
                    {
                        return Results.BadRequest("Ya existe una cuenta con ese correo o cédula en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Created($"/api/crearusuario/{nuevoUsuario}",
                        new { Respuesta = nuevoUsuario });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear un empleado:
            app.MapPost("/api/crearEmpleados", async (EmpleadoCreateDto empleadoDto,
                IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo, 
                string correoEmpleado, string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(empleadoDto.Activo.ToString()))
                    {
                        return Results.BadRequest("Activo es necesario.");
                    }


                    /* Es para validar si el departamento tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el -
                     * departamento es necesario. */
                    if (string.IsNullOrWhiteSpace(empleadoDto.Departamento))
                    {
                        return Results.BadRequest("El departamento es necesario.");
                    }

                    /* Aquí lo que se indica es que llama al método: "CrearEmpleado()" -
                     * para poder crear un empleado con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoEmpleado" es nulo, entonces -
                     * quiere decir que no se pudo crear ese empleado en la BD. Por -
                     * lo tanto se manda un error al empleado respectivamente. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    int idUsuario = Usuario!.Result!.Id;

                    var nuevoEmpleado = await empleadoRepo.CrearEmpleado(empleadoDto, idUsuario);
                    if (nuevoEmpleado == null)
                    {
                        return Results.BadRequest("No se pudo crear el empleado.");
                    }

                    /* También se valida si en la variable: "nuevoEmpleado" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un usuario con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (nuevoEmpleado == false)
                    {
                        return Results.BadRequest("Ya existe ese empleado en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Created($"/api/crearEmpleados/{nuevoEmpleado}",
                        new { Respuesta = nuevoEmpleado });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear una pregunta y respuesta:
            app.MapPost("/api/crearFAQ", async (FAQCreateDto faqDto, IFAQRepository preguntasYrespuestasRepo,
                string tokenAcceso, IJwtRepository jwtRepo, IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {

                    /* Es para validar si la pregunta tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la pregunta es -
                     * necesaria. */
                    if (string.IsNullOrWhiteSpace(faqDto.Pregunta))
                    {
                        return Results.BadRequest("La pregunta es necesaria.");
                    }


                    /* Es para validar si la respuesta tiene datos, si es nulo o esta en -
                     * blanco entonces el API tendria que dar un mensaje indicando que la -
                     * respuesta es necesaria. */
                    if (string.IsNullOrWhiteSpace(faqDto.Respuesta))
                    {
                        return Results.BadRequest("La respuesta es necesaria.");
                    }


                    /* Es para validar si el tipo de prioridad tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el - 
                     * tipo de prioridad es necesario. */
                    if (string.IsNullOrWhiteSpace(faqDto.Tipo_Prioridad))
                    {
                        return Results.BadRequest("El tipo de prioridad es necesario.");
                    }



                    /* Aquí lo que se indica es que llama al método: "CrearFAQ()" -
                     * para poder crear un usuario con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoUsuario" es nulo, entonces -
                     * quiere decir que no se pudo crear ese usuario en la BD. Por -
                     * lo tanto se manda un error al usuario respectivamente. */
                    string correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    int idUsuario = Usuario!.Result!.Id;

                    var nuevoFAQ = await preguntasYrespuestasRepo.CrearFAQ(faqDto, idUsuario);
                    if (nuevoFAQ == null)
                    {
                        return Results.BadRequest("No se pudo crear la pregunta y respuesta.");
                    }

                    /* También se valida si en la variable: "nuevoFAQ" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (nuevoFAQ == false)
                    {
                        return Results.BadRequest("Ya existe esa pregunta en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Created($"/api/crearFAQ/{nuevoFAQ}", 
                        new { Respuesta = nuevoFAQ });
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para enviar un correo de recuperación a los usuarios:
            app.MapPost("/api/enviarcorreo/{correo:required}", async (string correo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Es para validar si el correo tiene datos, si es nulo o esta en blanco -
                 * entonces el API tendria que dar un mensaje indicando que el correo es -
                 * necesario. */
                if (string.IsNullOrWhiteSpace(correo))
                {
                    return Results.BadRequest("El correo es necesario.");
                }


                /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama al método -
                 * para enviar el correo. Ahora, si en la variable: "respuestaRecuperacion" es nulo, -
                 * entonces quiere decir que no hay nada en la BD. Por lo tanto se manda un error al -
                 * usuario respectivamente. */
                var respuestaRecuperacion = await usuarioRepo.EnviarCorreo(correo);
                if (respuestaRecuperacion == null)
                {
                    return Results.BadRequest("No se pudo recuperar la cuenta.");
                }

                /* También se valida si en la variable: "respuestaRecuperacion" es igual -
                 * a false, y si lo es entonces quiere decir que ese correo no es valido -
                 * en la base de datos. Por lo tanto se manda un error al usuario respectivamente. */
                if (respuestaRecuperacion == false)
                {
                    return Results.BadRequest("El correo electrónico que fue proporcionado no es válido.");
                }


                /* Si no hubo ningún problema, entonces indicaria que el correo electrónico -
                 * proporcionado ha sido el correcto y que enviaria un código de recuperación -
                 * a dicho correo respectivo. Además de indicar el código: #200. */
                return Results.Ok(respuestaRecuperacion);
            });


            //Ruta (tipo: POST) del API que sirve para crear los permisos de autorización a los usuarios:
            app.MapPost("/api/crearPermisos", async (PermisoCreateDto permisoDto, string correoUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, IPermisoRepository permisoRepo, 
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Es para validar si el permiso de: "Leer" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Leer" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Leer.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Crear" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Crear" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Crear.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Actualizar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Actualizar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Actualizar.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Eliminar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Eliminar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Eliminar.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }


                    /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                     * al método para crear los permisos del usuario. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo electrónico en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;
                    var nuevoIdPermiso = await permisoRepo.CrearPermisos_Usuario(permisoDto, idUsuario);

                    //Si la variable nuevoIdPermiso es nulo quiere decir que no hay nada en la BD.
                    if (nuevoIdPermiso == null)
                    {
                        return Results.BadRequest("No se pudo crear los permisos.");
                    }

                    /* Si la variable nuevoIdPermiso es igual a 0 quiere decir que ya existe esos permisos -
                     * con el usuario en la BD. */
                    if (nuevoIdPermiso == false)
                    {
                        return Results.BadRequest("Ya existe un usuario con esos permisos.");
                    }

                    /* Si no hubo ningún problema, entonces dara el resultado como creado (que sería -
                     * el código: #201), lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente. */
                    return Results.Created($"/api/crearPermisos/{nuevoIdPermiso}",
                        new { Id = nuevoIdPermiso });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear los permisos a los empleados:
            app.MapPost("/api/crearPermisosTiempo", async (PermisoTiempoCreateDto tiempoDto, string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, IPermisoTiempoRepository tiempoRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }

                    /* Es para validar si el permiso de: "Leer" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Leer" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Tipo_Permiso))
                    {
                        return Results.BadRequest("¡ERROR: El tipo de permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Crear" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Crear" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Descripcion))
                    {
                        return Results.BadRequest("¡ERROR: La descripción no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Actualizar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Actualizar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Asignacion.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: La fecha de asignación no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Eliminar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Eliminar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Finalizacion.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: La fecha de finalización no puede ser nulo!");
                    }


                    /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                     * al método para crear los permisos del usuario. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                       return Results.BadRequest("No existe ese empleado en el sistema.");
                    }
                    
                    int idEmpleado = Empleado!.Result!.Id;
                    var nuevoIdTiempo = await tiempoRepo.CrearPermisosTiempo(tiempoDto, idEmpleado);

                    //Si la variable nuevoIdPermiso es nulo quiere decir que no hay nada en la BD.
                    if (nuevoIdTiempo == null)
                    {
                        return Results.BadRequest("No se pudo crear el permiso de tiempo.");
                    }

                    /* Si la variable nuevoIdPermiso es igual a 0 quiere decir que ya existe esos permisos -
                     * con el usuario en la BD. */
                    if (nuevoIdTiempo == false)
                    {
                        return Results.BadRequest("Ya existe un usuario con ese permiso de tiempo.");
                    }

                    /* Si no hubo ningún problema, entonces dara el resultado como creado (que sería -
                     * el código: #201), lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente. */
                    return Results.Created($"/api/crearPermisosTiempo/{nuevoIdTiempo}",
                        new { Id = nuevoIdTiempo });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para crear los salarios a los empleados:
            app.MapPost("/api/crearSalarios", async (SalarioCreateDto salarioDto, string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, ISalarioRepository salarioRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }

                    /* Es para validar si el permiso de: "Leer" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Leer" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(salarioDto.Fecha_Entrega))
                    {
                        return Results.BadRequest("¡ERROR: La fecha de entrega no puede ser nula!");
                    }

                    /* Es para validar si el permiso de: "Crear" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Crear" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(salarioDto.Salario.ToString()) || Regex.IsMatch(salarioDto.Salario.ToString(), "@\"^\\d+.\\d{2}$"))
                    {
                        return Results.BadRequest("¡ERROR: El salario no puede ser nulo o esta incorrecto!");
                    }

                    /* Es para validar si el permiso de: "Actualizar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Actualizar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(salarioDto.Descripcion))
                    {
                        return Results.BadRequest("¡ERROR: La descripcion no puede ser nula!");
                    }


                    /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                     * al método para crear los permisos del usuario. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;
                    var nuevoIdSalario = await salarioRepo.CrearSalarios(salarioDto, idEmpleado);

                    //Si la variable nuevoIdPermiso es nulo quiere decir que no hay nada en la BD.
                    if (nuevoIdSalario == null)
                    {
                        return Results.BadRequest("No se pudo crear el salario.");
                    }

                    /* Si la variable nuevoIdPermiso es igual a 0 quiere decir que ya existe esos permisos -
                     * con el usuario en la BD. */
                    if (nuevoIdSalario == false)
                    {
                        return Results.BadRequest("Ya existe un usuario con ese salario.");
                    }

                    /* Si no hubo ningún problema, entonces dara el resultado como creado (que sería -
                     * el código: #201), lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente. */
                    return Results.Created($"/api/crearSalarios/{nuevoIdSalario}",
                        new { Id = nuevoIdSalario });
                }

                return Results.Unauthorized();
            }).RequireAuthorization();





            //                    |=============| PUT |=============|

            //Ruta (tipo: PUT) del API que sirve para actualizar las contraseñas de los usuarios:
            app.MapPut("/api/actualizarcontraseña", async (ExtensionUsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Es para validar si el correo tiene datos, si es nulo o esta en blanco -
                 * entonces el API tendria que dar un mensaje indicando que el correo es -
                 * necesario. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico))
                {
                    return Results.BadRequest("El correo es necesario.");
                }


                /* Es para validar si la contraseña tiene datos, si es nulo o esta en blanco -
                 * o incluso si no cumple con la cantidad minima de digitos (que es 12) - 
                 * entonces el API tendria que dar un mensaje indicando que la contraseña - 
                 * es necesaria. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || usuarioDto.Contraseña.Trim().Length < 12)
                {
                    return Results.BadRequest("La contraseña es necesaria.");
                }


                /* Aquí lo que se indica es que llama al método: "ActualizarContraseñaUsuario()" -
                 * para poder actualizar la contraseña antigua del usuario.
                 * 
                 * Ahora, si en la variable: "respuestaActualizacion" es falso, entonces quiere -
                 * decir que no se pudo cambiar la contraseña. Por lo tanto se manda un error al -
                 * usuario respectivamente. */
                var respuestaActualizacion = await usuarioRepo.ActualizarContraseñaUsuario(usuarioDto.Contraseña, usuarioDto.Correo_Electronico);
                if (respuestaActualizacion != true)
                {
                    return Results.BadRequest("No se pudo cambiar la contraseña.");
                }


                /* Si no hubo ningún problema, entonces indicaria que se pudo cambiar la -
                 * contraseña de ese usuario. Además de indicar el código: #200. */
                return Results.Ok(respuestaActualizacion);
            });


            //Ruta (tipo: PUT) del API que sirve para actualizar a los usuarios:
            app.MapPut("/api/actualizarUsuario", async (UsuarioCreateDto usuarioDto, 
                IUsuarioRepository usuarioRepo, string tokenAcceso, string cedula, 
                IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Es para validar si el nombre tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el nombre es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Nombre))
                    {
                        return Results.BadRequest("El nombre es necesario.");
                    }


                    /* Es para validar si el primer apellido tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el -
                     * primer apellido es necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_1))
                    {
                        return Results.BadRequest("El primer apellido es necesario.");
                    }


                    /* Es para validar si el segundo apellido tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el - 
                     * segundo apellido es necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_2))
                    {
                        return Results.BadRequest("El segundo apellido es necesario.");
                    }


                    /* Es para validar si la edad tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la edad es -
                     * necesaria.
                     * 
                     * De igual manera haria lo mismo si detecta que la edad lo dejaron en cero, -
                     * o si ponen una edad mayor a 99 (lo que significaria una edad de 3 digitos) -
                     * respectivamente. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Edad.ToString()) || usuarioDto.Edad == 0 || usuarioDto.Edad > 99)
                    {
                        return Results.BadRequest("La edad es necesaria.");
                    }


                    /* Es para validar si la cédula tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la cédula es -
                     * necesaria.
                     * 
                     * De igual manera haria lo mismo si detecta que la cédula es mayor a 12 digitos, -
                     * ya que en Costa Rica hay un tamaño definido para la cédula respectivamente. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Cedula) || usuarioDto.Cedula.Trim().Length > 12 || string.IsNullOrWhiteSpace(cedula) || cedula.Trim().Length > 12)
                    {
                        return Results.BadRequest("La cédula es necesaria.");
                    }


                    /* Es para validar si el correo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el correo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico))
                    {
                        return Results.BadRequest("El correo es necesario.");
                    }


                    /* Es para validar si la contraseña tiene datos, si es nulo o esta en blanco -
                     * o incluso si no cumple con la cantidad minima de digitos (que es 12) - 
                     * entonces el API tendria que dar un mensaje indicando que la contraseña - 
                     * es necesaria. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || usuarioDto.Contraseña.Trim().Length < 12)
                    {
                        return Results.BadRequest("La contraseña es necesaria.");
                    }


                    /* Es para validar si el rol tiene datos, si es nulo o esta en blanco
                     * entonces el API tendria que dar un mensaje indicando que el rol es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(usuarioDto.Id_Rol.ToString()))
                    {
                        return Results.BadRequest("El rol es necesario.");
                    }


                    /* Aquí lo que se indica es que llama al método: "CrearEmpleado()" -
                     * para poder crear un empleado con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoEmpleado" es nulo, entonces -
                     * quiere decir que no se pudo crear ese empleado en la BD. Por -
                     * lo tanto se manda un error al empleado respectivamente. */
                    var resultadoUsuario = await usuarioRepo.ActualizarUsuario(usuarioDto, cedula);
                    if (resultadoUsuario == null)
                    {
                        return Results.BadRequest("No se pudo actualizar el usuario.");
                    }

                    /* También se valida si en la variable: "resultadoUsuario" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (resultadoUsuario == false)
                    {
                        return Results.BadRequest("No existe ese usuario en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoUsuario);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para crear los permisos de autorización a los usuarios:
            app.MapPut("/api/actualizarPermisos", async (PermisoCreateDto permisoDto, string correoUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, IPermisoRepository permisoRepo, IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Es para validar si el permiso de: "Leer" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Leer" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Leer.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Crear" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Crear" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Crear.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Actualizar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Actualizar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Actualizar.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Eliminar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Eliminar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(permisoDto.Eliminar.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                    }


                    /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                     * al método para crear los permisos del usuario. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;
                    var resultadoPermiso = await permisoRepo.ActualizarPermisos_Usuario(permisoDto, idUsuario);

                    //Si la variable nuevoIdPermiso es nulo quiere decir que no hay nada en la BD.
                    if (resultadoPermiso == null)
                    {
                        return Results.BadRequest("No se pudo actualizar los permisos del usuario.");
                    }

                    /* Si la variable nuevoIdPermiso es igual a 0 quiere decir que ya existe esos permisos -
                     * con el usuario en la BD. */
                    if (resultadoPermiso == false)
                    {
                        return Results.BadRequest("No existe ese usuario en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoPermiso);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para crear los permisos de tiempo a los usuarios:
            app.MapPut("/api/actualizarPermisosTiempo", async (PermisoTiempoCreateDto tiempoDto, string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, IPermisoTiempoRepository tiempoRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesario.");
                    }

                    /* Es para validar si el permiso de: "Leer" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Leer" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Tipo_Permiso))
                    {
                        return Results.BadRequest("¡ERROR: El tipo de permiso no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Crear" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Crear" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Descripcion))
                    {
                        return Results.BadRequest("¡ERROR: La descripción no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Actualizar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Actualizar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Asignacion.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: La fecha de asignación no puede ser nulo!");
                    }

                    /* Es para validar si el permiso de: "Eliminar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Eliminar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(tiempoDto.Fecha_Finalizacion.ToString()))
                    {
                        return Results.BadRequest("¡ERROR: La fecha de finalización no puede ser nulo!");
                    }


                    /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                     * al método para crear los permisos del usuario. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }


                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;
                    var resultadoPermisoTiempo = await tiempoRepo.ActualizarPermisosTiempo(tiempoDto, idEmpleado);

                    //Si la variable nuevoIdPermiso es nulo quiere decir que no hay nada en la BD.
                    if (resultadoPermisoTiempo == null)
                    {
                        return Results.BadRequest("No se pudo actualizar el permiso de tiempo.");
                    }

                    /* Si la variable nuevoIdPermiso es igual a 0 quiere decir que ya existe esos permisos -
                     * con el usuario en la BD. */
                    if (resultadoPermisoTiempo == false)
                    {
                        return Results.BadRequest("No existe ese usuario en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoPermisoTiempo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar los salarios:
            app.MapPut("/api/actualizarSalarios", async (SalarioCreateDto salarioDto, string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, ISalarioRepository salarioRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesario.");
                    }

                    /* Es para validar si el permiso de: "Leer" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Leer" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(salarioDto.Fecha_Entrega))
                    {
                        return Results.BadRequest("¡ERROR: La fecha de entrega no puede ser nula!");
                    }

                    /* Es para validar si el permiso de: "Crear" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Crear" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(salarioDto.Salario.ToString()) || Regex.IsMatch(salarioDto.Salario.ToString(), "@\"^\\d+\\.\\d{2}$\""))
                    {
                        return Results.BadRequest("¡ERROR: El salario no puede ser nulo o esta incorrecto!");
                    }

                    /* Es para validar si el permiso de: "Actualizar" tiene datos, si es nulo entonces -
                     * el API tendria que dar un mensaje indicando que el permiso de: "Actualizar" es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(salarioDto.Descripcion))
                    {
                        return Results.BadRequest("¡ERROR: La descripcion no puede ser nula!");
                    }


                    /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                     * al método para crear los permisos del usuario. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;
                    var resultadoSalario = await salarioRepo.ActualizarSalarios(salarioDto, idEmpleado);

                    //Si la variable nuevoIdPermiso es nulo quiere decir que no hay nada en la BD.
                    if (resultadoSalario == null)
                    {
                        return Results.BadRequest("No se pudo actualizar el salario.");
                    }

                    /* Si la variable nuevoIdPermiso es igual a 0 quiere decir que ya existe esos permisos -
                     * con el usuario en la BD. */
                    if (resultadoSalario == false)
                    {
                        return Results.BadRequest("No existe ese usuario en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoSalario);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para cambiar (o actualizar) las fotos de perfil:
            app.MapPut("/api/cambiarFoto", async (string tokenAcceso, HttpRequest peticion,
                IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2 || RolUsuario == 3)
                {
                    /* Aqui lo que se esta haciendo es que con la variable: "peticion", nos -
                     * ayudaria a obtener la petición (o solicitud) HTTP que fue solicitado -
                     * por el usuario, que en este caso es cambiar la foto de perfil. Luego -
                     * de eso, con esa variable podriamos leer el cuerpo de la solicitud (que -
                     * seria el contenido que viene) a traves del comando: ReadFormAsync(), y -
                     * lo devolveria como una colección: Task<IFormCollection>.
                     * 
                     *
                     * Ahora, gracias a esa colección, podriamos finalmente extraer la imagen -
                     * que esta almacenada en el campo: "Imagen_Perfil", de forma que ahora se -
                     * pueda hacer el procedimiento necesario para cambiar la imagen de perfil -
                     * respectivamente. */
                    var archivo = await peticion.ReadFormAsync();
                    var imagenPerfil = archivo["Imagen_Perfil"].ToString();

                    /* Es para validar si la imagen tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la imagen es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(imagenPerfil))
                    {
                        return Results.BadRequest("La foto es necesaria.");
                    }

                    /* Aqui lo que se hace es obtener el claim que tiene como nombre: "correo" y -
                     * guardarlo en la variable: "correoUsuario". Esto se hace porque se necesita -
                     * enviar ese correo del usuario para así ver si se puede cambiar o no la foto -
                     * de perfil. */
                    var correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;

                    /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama al método -
                     * para actualizar (o cambiar) la foto de perfil del usuario. Ahora, si en la variable: -
                     * "respuesta" es nulo, entonces quiere decir que no se pudo cambiar la imagen en la BD. 
                     * 
                     * Por lo tanto se manda un error al usuario respectivamente. */
                    var respuesta = await usuarioRepo.ActualizarFotoPerfil(imagenPerfil, correoUsuario);
                    if (respuesta == null)
                    {
                        return Results.BadRequest("No se pudo actualizar la foto de perfil.");
                    }

                    /* Por otro lado, también se valida si en la variable: "respuesta" es igual -
                     * a falso, y si lo es entonces quiere decir que ese correo no es valido en -
                     * la base de datos. Por lo tanto se manda un error al usuario respectivamente. */
                    if (respuesta == false)
                    {
                        return Results.BadRequest("No existe ese usuario en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces indicaria que el correo electrónico -
                     * proporcionado ha sido el correcto y que enviaria una respuesta sobre la -
                     * actualización de dicho foto de perfil. Además de indicar el código: #200. */
                    return Results.Ok(respuesta);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar las preguntas y respuestas:
            app.MapPut("/api/actualizarFAQ", async (FAQCreateDto faqDto, string preguntaActual, IFAQRepository preguntasYrespuestasRepo,
                string tokenAcceso, IJwtRepository jwtRepo, IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si la pregunta tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la pregunta es -
                     * necesaria. */
                    if (string.IsNullOrWhiteSpace(faqDto.Pregunta))
                    {
                        return Results.BadRequest("La pregunta es necesaria.");
                    }


                    /* Es para validar si la respuesta tiene datos, si es nulo o esta en -
                     * blanco entonces el API tendria que dar un mensaje indicando que la -
                     * respuesta es necesaria. */
                    if (string.IsNullOrWhiteSpace(faqDto.Respuesta))
                    {
                        return Results.BadRequest("La respuesta es necesaria.");
                    }


                    /* Es para validar si el tipo de prioridad tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el - 
                     * tipo de prioridad es necesario. */
                    if (string.IsNullOrWhiteSpace(faqDto.Tipo_Prioridad))
                    {
                        return Results.BadRequest("El tipo de prioridad es necesaria.");
                    }



                    /* Aquí lo que se indica es que llama al método: "CrearFAQ()" -
                     * para poder crear un usuario con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoUsuario" es nulo, entonces -
                     * quiere decir que no se pudo crear ese usuario en la BD. Por -
                     * lo tanto se manda un error al usuario respectivamente. */
                    string correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;
                    var resultadoFAQ = await preguntasYrespuestasRepo.ActualizarFAQ(faqDto, idUsuario, preguntaActual);
                    if (resultadoFAQ == null)
                    {
                        return Results.BadRequest("No se pudo actualizar el FAQ.");
                    }

                    /* También se valida si en la variable: "nuevoFAQ" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (resultadoFAQ == false)
                    {
                        return Results.BadRequest("No existe ese FAQ en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoFAQ);
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: PUT) del API que sirve para actualizar a los empleados:
            app.MapPut("/api/actualizarEmpleado", async (EmpleadoCreateDto empleadoDto,
                IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo,
                string correoEmpleado, string tokenAcceso, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {
                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(empleadoDto.Activo.ToString()))
                    {
                        return Results.BadRequest("Activo es necesario.");
                    }


                    /* Es para validar si el departamento tiene datos, si es nulo o esta -
                     * en blanco entonces el API tendria que dar un mensaje indicando que el -
                     * departamento es necesario. */
                    if (string.IsNullOrWhiteSpace(empleadoDto.Departamento))
                    {
                        return Results.BadRequest("El departamento es necesario.");
                    }

                    /* Aquí lo que se indica es que llama al método: "CrearEmpleado()" -
                     * para poder crear un empleado con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoEmpleado" es nulo, entonces -
                     * quiere decir que no se pudo crear ese empleado en la BD. Por -
                     * lo tanto se manda un error al empleado respectivamente. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;
                    var resultadoEmpleado = await empleadoRepo.ActualizarEmpleado(empleadoDto, idUsuario);
                    if (resultadoEmpleado == null)
                    {
                        return Results.BadRequest("No se pudo actualizar el empleado(a).");
                    }

                    /* También se valida si en la variable: "resultadoEmpleado" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (resultadoEmpleado == false)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoEmpleado);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();





            //                     |=============| DELETE |=============|

            //Ruta (tipo: DELETE) del API que sirve para eliminar la pregunta y respuesta:
            app.MapDelete("/api/eliminarFAQ", async (string preguntaFAQ, IFAQRepository preguntasYrespuestasRepo,
                string tokenAcceso, IJwtRepository jwtRepo, IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si la pregunta tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que la pregunta es -
                     * necesaria. */
                    if (string.IsNullOrWhiteSpace(preguntaFAQ))
                    {
                        return Results.BadRequest("La pregunta es necesaria.");
                    }


                    /* Aquí lo que se indica es que llama al método: "CrearFAQ()" -
                     * para poder crear un usuario con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoUsuario" es nulo, entonces -
                     * quiere decir que no se pudo crear ese usuario en la BD. Por -
                     * lo tanto se manda un error al usuario respectivamente. */
                    string correoUsuario = respuestaValidarToken.FindFirst("correo")!.Value;
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;
                    var resultadoFAQ = await preguntasYrespuestasRepo.EliminarFAQ(preguntaFAQ, idUsuario);
                    if (resultadoFAQ == null)
                    {
                        return Results.BadRequest("No se pudo eliminar el FAQ seleccionado.");
                    }

                    /* También se valida si en la variable: "nuevoFAQ" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (resultadoFAQ == false)
                    {
                        return Results.BadRequest("No existe ese FAQ en el sistema.");
                    }

                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoFAQ);
                }
                
                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: DELETE) del API que sirve para eliminar a un empleado:
            app.MapDelete("/api/eliminarEmpleado", async (string correoEmpleado, string tokenAcceso,
                IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo, IJwtRepository jwtRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(correoEmpleado))
                    {
                        return Results.BadRequest("El correo es necesario.");
                    }

                    /* Aquí lo que se indica es que llama al método: "CrearEmpleado()" -
                     * para poder crear un empleado con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoEmpleado" es nulo, entonces -
                     * quiere decir que no se pudo crear ese empleado en la BD. Por -
                     * lo tanto se manda un error al empleado respectivamente. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCorreo(correoEmpleado)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe ese correo en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;
                    var resultadoEmpleado = await empleadoRepo.EliminarEmpleado(idUsuario);
                    var resultadoUsuario = await usuarioRepo.EliminarUsuario(idUsuario);

                    if (resultadoEmpleado == null && resultadoUsuario == null)
                    {
                        return Results.BadRequest("No se pudo eliminar el empleado(a) seleccionado.");
                    }

                    /* También se valida si en la variable: "resultadoEmpleado" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (resultadoEmpleado == false && resultadoUsuario == false)
                    {
                        return Results.BadRequest("No existe ese empleado(a) en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoEmpleado);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: DELETE) del API que sirve para eliminar un permiso de tiempo:
            app.MapDelete("/api/eliminarPermisosTiempo", async (string cedulaUsuario, string tokenAcceso, IJwtRepository jwtRepo, 
                IPermisoTiempoRepository tiempoRepo, IEmpleadoRepository empleadoRepo, IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesario.");
                    }

                    /* Aquí lo que se indica es que llama al método: "CrearEmpleado()" -
                     * para poder crear un empleado con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoEmpleado" es nulo, entonces -
                     * quiere decir que no se pudo crear ese empleado en la BD. Por -
                     * lo tanto se manda un error al empleado respectivamente. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }


                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;
                    var resultadoPermisosTiempo = await tiempoRepo.EliminarPermisosTiempo(idEmpleado);

                    if (resultadoPermisosTiempo == null)
                    {
                        return Results.BadRequest("No se pudo eliminar el permiso de tiempo seleccionado.");
                    }

                    /* También se valida si en la variable: "resultadoPermisosTiempo" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (resultadoPermisosTiempo == false)
                    {
                        return Results.BadRequest("No existe ese permiso de tiempo en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoPermisosTiempo);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: DELETE) del API que sirve para eliminar un salario:
            app.MapDelete("/api/eliminarSalarios", async (string cedulaUsuario,
                string tokenAcceso, IJwtRepository jwtRepo, ISalarioRepository salarioRepo, IEmpleadoRepository empleadoRepo,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Aqui lo que se hace es validar el token de acceso que se esta -
                 * pasando por parametro. 
                 *
                 * Ahora, si la respuesta da un nulo, entonces quiere decir que -
                 * ese token esta incorrecto o que ya paso su tiempo de vida, -
                 * por lo que se menciona que no esta autorizado. */
                var respuestaValidarToken = jwtRepo.validarTokenJWT(tokenAcceso);
                if (respuestaValidarToken == null)
                {
                    return Results.Unauthorized();
                }


                /* Aqui lo que se hace es obtener el claim que tiene como nombre: "rol" -
                 * y luego transformarlo en un formato entero.
                 * 
                 * Esto se hace porque se necesita validar si en el token de ese usuario, -
                 * el rol es de un empleado, administrador o moderador, y si no tiene, que -
                 * entonces no lo deje pasar y se indique que no esta autorizado para esta -
                 * función. */
                int RolUsuario = int.Parse(respuestaValidarToken.FindFirst("rol")!.Value);
                if (RolUsuario == 1 || RolUsuario == 2)
                {

                    /* Es para validar si el activo tiene datos, si es nulo o esta en blanco -
                     * entonces el API tendria que dar un mensaje indicando que el activo es -
                     * necesario. */
                    if (string.IsNullOrWhiteSpace(cedulaUsuario))
                    {
                        return Results.BadRequest("La cédula es necesario.");
                    }

                    /* Aquí lo que se indica es que llama al método: "CrearEmpleado()" -
                     * para poder crear un empleado con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoEmpleado" es nulo, entonces -
                     * quiere decir que no se pudo crear ese empleado en la BD. Por -
                     * lo tanto se manda un error al empleado respectivamente. */
                    Task<UsuarioEntitie?> Usuario = usuarioRepo.ObtenerUsuario_PorCedula(cedulaUsuario)!;
                    if (Usuario.Result == null)
                    {
                        return Results.BadRequest("No existe esa cédula en el sistema.");
                    }

                    int idUsuario = Usuario!.Result!.Id;

                    Task<EmpleadoEntitie?> Empleado = empleadoRepo.ObtenerEmpleado_PorIdUsuario(idUsuario)!;
                    if (Empleado.Result == null)
                    {
                        return Results.BadRequest("No existe ese empleado en el sistema.");
                    }

                    int idEmpleado = Empleado!.Result!.Id;
                    var resultadoSalario = await salarioRepo.EliminarSalarios(idEmpleado);

                    if (resultadoSalario == null)
                    {
                        return Results.BadRequest("No se pudo eliminar el salario seleccionado.");
                    }

                    /* También se valida si en la variable: "resultadoPermisosTiempo" es igual a falso, -
                     * y si lo es entonces quiere decir que ya existe un nuevoFAQ con ese correo -
                     * o contraseña en la BD. Por lo tanto se manda un error al usuario -
                     * respectivamente. */
                    if (resultadoSalario == false)
                    {
                        return Results.BadRequest("No existe ese salario en el sistema.");
                    }


                    /* Si no hubo ningún problema, entonces crearia el usuario y se mandaria -
                     * un código: #201, lo que indicaria que la solicitud POST se pudo realizar -
                     * correctamente.*/
                    return Results.Ok(resultadoSalario);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();



            //Comando para ejecutar el proyecto:
            app.Run();
        }
    }
}
