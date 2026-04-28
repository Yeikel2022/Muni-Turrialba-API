
using MuniTurrialbaAPI.Models;
using MuniTurrialbaAPI.Repositories;

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

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            //                |=============| RUTAS DEL API |=============|

            //Ruta (tipo: GET) del API que sirve para traer todos los usuarios:
            app.MapGet("/api/usuarios", async (IUsuarioRepository usuarioRepo) => 
            {
                /* Aquí lo que se indica es que llama al método: "ObtenerUsuarios()" -
                 * para traer a todos los usuarios que hay en la BD. */
                var usuarios = await usuarioRepo.ObtenerUsuarios();

                /* Si la variable: "usuarios" es nulo, quiere decir que no hay nada en la BD. 
                 * Por lo tanto se manda un error al usuario respectivamente. */
                if (usuarios is null)
                {
                    return Results.NotFound("¡ERROR: No se pudieron obtener los usuarios!");
                }


                /* Si no hubo ningún problema, entonces se mostrarian los usuarios respectivamente, -
                 * además de indicar el código, que seria un código #200.*/
                return Results.Ok(usuarios);
            });


            //Ruta (tipo: GET) del API que sirve para traer un usuario por medio de un correo:
            app.MapGet("/api/usuario/{correo:required}", async (string correo,
                IUsuarioRepository usuarioRepo) => 
            {
                var usuario = await usuarioRepo.ObtenerUsuario_PorCorreo(correo);

                /* Si la variable: "usuario" es nulo, quiere decir que no hay nada en la BD. 
                 * Por lo tanto se manda un error al usuario respectivamente. */
                if (usuario is null)
                {
                    return Results.NotFound("¡ERROR: No se pudo obtener el usuario!");
                }

                //return usuario is not null ? Results.Ok(usuario) : Results.NotFound();
                return Results.Ok(usuario);
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
                    return Results.BadRequest("¡ERROR: El nombre es necesario!");
                }


                /* Es para validar si el primer apellido tiene datos, si es nulo o esta -
                 * en blanco entonces el API tendria que dar un mensaje indicando que el -
                 * primer apellido es necesario. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_1))
                {
                    return Results.BadRequest("¡ERROR: El primer apellido es necesario!");
                }


                /* Es para validar si el segundo apellido tiene datos, si es nulo o esta -
                 * en blanco entonces el API tendria que dar un mensaje indicando que el - 
                 * segundo apellido es necesario. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Apellido_2))
                {
                    return Results.BadRequest("¡ERROR: El segundo apellido es necesario!");
                }


                /* Es para validar si la edad tiene datos, si es nulo o esta en blanco -
                 * entonces el API tendria que dar un mensaje indicando que la edad es -
                 * necesaria.
                 * 
                 * De igual manera haria lo mismo si detecta que la edad lo dejaron en cero, -
                 * o si ponen una edad mayor a 99 (lo que significaria una edad de 3 digitos) -
                 * respectivamente. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Edad.ToString()) || 
                usuarioDto.Edad == 0 || usuarioDto.Edad > 99)
                {
                    return Results.BadRequest("¡ERROR: La edad es necesaria!");
                }


                /* Es para validar si la cédula tiene datos, si es nulo o esta en blanco -
                 * entonces el API tendria que dar un mensaje indicando que la cédula es -
                 * necesaria.
                 * 
                 * De igual manera haria lo mismo si detecta que la cedula es mayor a 12 digitos, -
                 * ya que en Costa Rica hay un tamaño definido para la cedula respectivamente. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Cedula) || usuarioDto.Cedula.Trim().Length > 12)
                {
                    return Results.BadRequest("¡ERROR: La cédula es necesaria!");
                }


                /* ******Es para validar si el correo tiene datos, si es nulo o esta en blanco
                 * entonces el API tendria que dar un mensaje indicando que el correo es -
                 * necesario. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico))
                {
                    return Results.BadRequest("¡ERROR: El correo es necesario!");
                }


                /* Es para validar si la contraseña tiene datos, si es nulo o esta en blanco -
                 * o incluso si no cumple con la cantidad minima de digitos (que es 12) - 
                 * entonces el API tendria que dar un mensaje indicando que la contraseña - 
                 * es necesaria. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) ||
                usuarioDto.Contraseña.Trim().Length < 12)
                {
                    return Results.BadRequest("¡ERROR: La contraseña es necesaria!");
                }


                /* Es para validar si el rol tiene datos, si es nulo o esta en blanco
                 * entonces el API tendria que dar un mensaje indicando que el rol es -
                 * necesario. */
                if (string.IsNullOrWhiteSpace(usuarioDto.Id_Rol.ToString()))
                {
                    return Results.BadRequest("¡ERROR: El rol es necesario!");
                }


                /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                 * al método para crear el usuario. */
                var nuevoIdUsuario = await usuarioRepo.CrearUsuario(usuarioDto);

                //Si la variable nuevoIdUsuario es nulo quiere decir que no hay nada en la BD.
                if (nuevoIdUsuario == null)
                {
                    return Results.BadRequest("¡ERROR: No se pudo crear el usuario!");
                }

                //Si la variable nuevoIdUsuario es nulo quiere decir que no hay nada en la BD.
                if (nuevoIdUsuario == 0)
                {
                    return Results.BadRequest("¡ERROR: No se pudo crear el usuario, debido a que ya existe una cuenta con ese correo o cedula " +
                        "dentro de la aplicación móvil!");
                }

                /* Si no hubo ningún problema, entonces dara el resultado como creado (que sería -
                 * el código: #201), lo que indicaria que la solicitud POST se pudo realizar -
                 * correctamente.*/
                return Results.Created($"/api/crearusuarios/{nuevoIdUsuario}", 
                    new { Id = nuevoIdUsuario });
            });

            
            //Ruta (tipo: POST) del API que sirve para enviar un correo:
            app.MapPost("/api/enviarcorreo/{correo:required}", async (string correo,
                IUsuarioRepository usuarioRepo) =>
            {

                /* ******Es para validar si el correo tiene datos, si es nulo o esta en blanco
                 * entonces el API tendria que dar un mensaje indicando que el correo es -
                 * necesario. */
                if (string.IsNullOrWhiteSpace(correo))
                {
                    return Results.BadRequest("¡ERROR: El correo es necesario!");
                }

                /* Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                 * al método para crear el usuario. */
                var respuestaRecuperacion = await usuarioRepo.EnviarCorreo(correo);

                //Si la variable respuestaRecuperacion es nulo quiere decir que no hay nada en la BD.
                if (respuestaRecuperacion.ToString() is null || int.Parse(respuestaRecuperacion) == 0)
                {
                    return Results.BadRequest("¡ERROR: No se pudo recuperar la cuenta!");
                }

                // Si la variable respuestaRecuperacion es igual a 2
                if (int.Parse(respuestaRecuperacion) == 2)
                {
                    return Results.BadRequest("¡ERROR: El correo electrónico que fue proporcionado no es válido!");
                }

                /* Si no hubo ningún problema, entonces dara el resultado como creado (que sería -
                 * el código: #201), lo que indicaria que la solicitud POST se pudo realizar -
                 * correctamente.*/
                return Results.Created($"/api/enviarcorreo/{respuestaRecuperacion}",
                    new { Id = int.Parse(respuestaRecuperacion) });
            });

            /*Ruta (tipo: POST) del API que sirve para enviar un correo:
            app.MapPost("/api/enviarcodigo/{codigo:required}", async (string codigo,
                IUsuarioRepository usuarioRepo) =>
            {

                // ******Es para validar si el correo tiene datos, si es nulo o esta en blanco
                 * entonces el API tendria que dar un mensaje indicando que el correo es -
                 * necesario.
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    return Results.BadRequest("¡ERROR: El código es necesario!");
                }

                // Si llega hasta aquí, quiere decir que todo esta bien, por lo que llama -
                 * al método para crear el usuario. 
                var respuestaRecuperacion = await usuarioRepo.VerificarCodigo(codigo);

                //Si la variable respuestaRecuperacion es nulo quiere decir que no hay nada en la BD.
                if (respuestaRecuperacion != true)
                {
                    return Results.BadRequest("¡ERROR: El código ingresado esta incorrecto!");
                }

                // Si no hubo ningún problema, entonces dara el resultado como creado (que sería -
                 * el código: #201), lo que indicaria que la solicitud POST se pudo realizar -
                 * correctamente.
                return Results.Created($"/api/enviarcodigo/{respuestaRecuperacion}",
                    new { RespuestaFinal = respuestaRecuperacion});
            });*/

            //Ruta (tipo: GET) del API que sirve para traer un usuario por medio de un correo:
            app.MapGet("/api/validarcodigo/{codigo:required}", (string codigo, 
                IUsuarioRepository usuarioRepo) =>
            {
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    return Results.BadRequest("¡ERROR: El código es necesario!");
                }

                var respuestaRecuperacion = usuarioRepo.VerificarCodigo(codigo);

                //Si la variable respuestaRecuperacion es nulo quiere decir que no hay nada en la BD.
                if (respuestaRecuperacion != true)
                {
                    return Results.BadRequest("¡ERROR: El código ingresado esta incorrecto!");
                }

                return Results.Ok(respuestaRecuperacion);
            });

            app.MapPut("/api/actualizarcontraseña", async (ExtensionUsuarioCreateDto usuarioDto,
                IUsuarioRepository usuarioRepo) =>
            {
                /* */
                if (string.IsNullOrWhiteSpace(usuarioDto.Correo_Electronico))
                {
                    return Results.BadRequest("¡ERROR: El correo es necesario!");
                }


                /*  */
                if (string.IsNullOrWhiteSpace(usuarioDto.Contraseña) || usuarioDto.Contraseña.Trim().Length < 12)
                {
                    return Results.BadRequest("¡ERROR: La contraseña es necesaria!");
                }

                var respuestaActualizacion = await usuarioRepo.ActualizarContraseñaUsuario(usuarioDto.Contraseña, 
                    usuarioDto.Correo_Electronico);

                //Si la variable respuestaRecuperacion es nulo quiere decir que no hay nada en la BD.
                if (respuestaActualizacion != true)
                {
                    return Results.BadRequest("¡ERROR: No se pudo cambiar la contraseña!");
                }

                return Results.Ok(respuestaActualizacion);
            });


            //Comando para ejecutar el proyecto:
            app.Run();
        }
    }
}
