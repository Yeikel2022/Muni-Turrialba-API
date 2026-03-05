
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

            /*|=======| RUTAS DEL API |=============|*/
            
            //Ruta (tipo: GET) del API que sirve para traer todos los usuarios:
            app.MapGet("/api/usuarios", async (IUsuarioRepository usuarioRepo) => 
            {
                var usuarios = await usuarioRepo.ObtenerUsuarios();
                return Results.Ok(usuarios);
            });


            //Ruta (tipo: GET) del API que sirve para traer un usuario por medio de un ID:
            /*app.MapGet("/api/usuarios/{id:int}", async (int id, IUsuarioRepository usuarioRepo) => 
            {
                var usuario = await usuarioRepo.ObtenerUsuario_PorId(id);
                return usuario is not null ? Results.Ok(usuario) : Results.NotFound();
            });*/


            //Ruta (tipo: POST) del API que sirve para crear un usuario:
            app.MapPost("/api/crearusuarios", async (UsuarioCreateDto usuarioDto, 
                IUsuarioRepository usuarioRepo) => 
            {
                if (string.IsNullOrWhiteSpace(usuarioDto.Nombre))
                {
                    return Results.BadRequest("Nombre es necesario.");
                }

                var nuevoIdUsuario = await usuarioRepo.CrearUsuario(usuarioDto);

                return Results.Created($"/api/crearusuarios/{nuevoIdUsuario}", new { Id = nuevoIdUsuario });
            });


            //Comando para ejecutar el proyecto:
            app.Run();
        }
    }
}
