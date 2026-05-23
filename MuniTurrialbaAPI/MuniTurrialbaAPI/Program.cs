
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MuniTurrialbaAPI.Models;
using MuniTurrialbaAPI.Repositories;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

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
                    return Results.BadRequest("El código ingresado esta incorrecto.");
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
                        return Results.NotFound("No se pudo obtener el código QR.");
                    }


                    /* Aquí lo que se indica es que si en la variable: "codigoQR" es falso, -
                     * entonces quiere decir que ese correo no existe en la BD. Por lo tanto -
                     * se manda un error al usuario respectivamente. */
                    if (codigoQR.ToString() == "false")
                    {
                        return Results.NotFound("El correo proporcionado esta incorrecto.");
                    }


                    /* Si no hubo ningún problema, entonces se crearia el código QR del usuario -
                     * respectivamente. Además de indicar un código #200. */
                    return Results.Ok(codigoQR);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();


            //Ruta (tipo: POST) del API que sirve para que el usuario pueda iniciar sesión:
            app.MapPost("/api/iniciarsesion", (ExtensionUsuarioCreateDto usuarioDto, IUsuarioRepository 
                usuarioRepo, IJwtRepository jwtRepo, IPermisoRepository permisoRepo) => 
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


                /* Y una vez colocado todo lo necesario en los claims, entonces se pasarian -
                 * al método: crearTokenJWT(), para que ahora así se pueda crear el token JWT -
                 * del usuario, y luego se pueda mandar en el result respectivamente. */
                var respuestaToken = jwtRepo.crearTokenJWT(claims_TokenGenerar);                              
                
                return Results.Created($"/api/iniciarsesion/{respuestaToken}", 
                    new { TokenAcceso = respuestaToken });
            });


            //Ruta (tipo: POST) del API que sirve para crear un usuario:
            app.MapPost("/api/crearusuarios", async (UsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo) =>
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


                    /* Aquí lo que se indica es que llama al método: "CrearUsuario()" -
                     * para poder crear un usuario con todos los datos que se estan -
                     * pasando por medio de la variable: usuarioDto.
                     * 
                     * Ahora, si en la variable: "nuevoUsuario" es nulo, entonces -
                     * quiere decir que no se pudo crear ese usuario en la BD. Por -
                     * lo tanto se manda un error al usuario respectivamente. */
                    var nuevoUsuario = await usuarioRepo.CrearUsuario(usuarioDto);
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


            //Ruta (tipo: POST) del API que sirve para enviar un correo:
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


            /*Ruta (tipo: POST) del API que sirve para crear los permisos al usuario:
            app.MapPost("/api/crearPermisos", async (PermisoCreateDto permisoDto,
                IUsuarioRepository usuarioRepo) =>
            {
                /* Es para validar si el permiso de: "Leer" tiene datos, si es nulo entonces -
                 * el API tendria que dar un mensaje indicando que el permiso de: "Leer" es -
                 * necesario.
                if (string.IsNullOrWhiteSpace(permisoDto.Leer.ToString()))
                {
                    return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                }

                /* Es para validar si el permiso de: "Crear" tiene datos, si es nulo entonces -
                 * el API tendria que dar un mensaje indicando que el permiso de: "Crear" es -
                 * necesario. 
                if (string.IsNullOrWhiteSpace(permisoDto.Crear.ToString()))
                {
                    return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                }

                /* Es para validar si el permiso de: "Actualizar" tiene datos, si es nulo entonces -
                 * el API tendria que dar un mensaje indicando que el permiso de: "Actualizar" es -
                 * necesario. 
                if (string.IsNullOrWhiteSpace(permisoDto.Actualizar.ToString()))
                {
                    return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                }

                /* Es para validar si el permiso de: "Eliminar" tiene datos, si es nulo entonces -
                 * el API tendria que dar un mensaje indicando que el permiso de: "Eliminar" es -
                 * necesario. 
                if (string.IsNullOrWhiteSpace(permisoDto.Eliminar.ToString()))
                {
                    return Results.BadRequest("¡ERROR: El permiso no puede ser nulo!");
                }

                /* Es para validar si el usuario que se paso tiene datos, si es nulo entonces -
                 * el API tendria que dar un mensaje indicando que el usuario es necesario. 
                if (string.IsNullOrWhiteSpace(permisoDto.Id_Usuario.ToString()))
                {
                    return Results.BadRequest("¡ERROR: El usuario no puede ser nulo!");
                }

                /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                 * al método para crear los permisos del usuario. 
                var nuevoIdPermiso = await usuarioRepo.CrearPermiso_Usuario(permisoDto);

                //Si la variable nuevoIdPermiso es nulo quiere decir que no hay nada en la BD.
                if (nuevoIdPermiso == null)
                {
                    return Results.BadRequest("¡ERROR: No se pudo crear los permisos del usuario!");
                }

                /* Si la variable nuevoIdPermiso es igual a 0 quiere decir que ya existe esos permisos -
                 * con el usuario en la BD. 
                if (nuevoIdPermiso == false)
                {
                    return Results.BadRequest("¡ERROR: No se pudo crear los permisos a ese usuario, debido a que ya se les asigno dentro de la aplicación móvil!");
                }

                /* Si no hubo ningún problema, entonces dara el resultado como creado (que sería -
                 * el código: #201), lo que indicaria que la solicitud POST se pudo realizar -
                 * correctamente.
                return Results.Created($"/api/crearPermisos/{nuevoIdPermiso}",
                    new { Id = nuevoIdPermiso });
            });*/


            //Ruta (tipo: PUT) del API que sirve para actualizar la contraseña del usuario:
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


            //Ruta (tipo: PUT) del API que sirve para cambiar la foto de perfil:
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
                        return Results.BadRequest("El correo electrónico que fue proporcionado no es válido.");
                    }


                    /* Si no hubo ningún problema, entonces indicaria que el correo electrónico -
                     * proporcionado ha sido el correcto y que enviaria una respuesta sobre la -
                     * actualización de dicho foto de perfil. Además de indicar el código: #200. */
                    return Results.Ok(respuesta);
                }

                return Results.Unauthorized();
            }).RequireAuthorization();



            //Comando para ejecutar el proyecto:
            app.Run();
        }
    }
}
