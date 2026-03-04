using Dapper;
using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        /*
         * |==============| Zona de conexión a la BD |==============|  
         */
        //Es para el método con la asignación: 1.1
        private readonly string _connectionString;
        
        //Para los metodos de crear y obtener un IdUsuario por medio de la cédula.
        int resultado_cedula;

        /* Asignación: 1.1
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public UsuarioRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
        
        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

     //|========================================================================================|
        
        /* Este método sirve para crear un usuario en la base de datos. */
        public async Task<int> CrearUsuario(UsuarioCreateDto userdto)
        {
            //Es para saber si ese usuario existe en la BD, por medio de la cédula.
            bool resultado = ObtenerIDUsuario_PorCedula(userdto.Cedula);
            
            //Crea la conexión.
            using var conexionBD = CreateConnection();

            //Si el resultado es distinto de falso, quiere decir que ese usuario no existe.
            if (resultado != false)
            {
                /* Es similar al primer IF, pero se pone para verificar si realmente -
                 * ese usuario no existe, es como un medio de precaución. */
                if (resultado_cedula == 0)
                {
                    //Para ejecutar el procedimiento almacenado.
                    var nuevoUsuario = await conexionBD.QuerySingleAsync<dynamic>(
                        //Procedimiento almacenado:
                        "PROCED_CrearUsuarios",
                        /*Se coloca los parametros*/
                        new
                        {
                            userdto.Nombre,
                            userdto.Apellido_1,
                            userdto.Apellido_2,
                            userdto.Edad,
                            userdto.Cedula,
                            userdto.Telefono,
                            userdto.Correo_Electronico,
                            userdto.Contraseña,
                            userdto.Fecha_Creacion,
                            userdto.Imagen_Perfil,
                            userdto.Id_Rol
                        },
                        commandType: System.Data.CommandType.StoredProcedure);

                    //Esto es para mostrar al API el id del usuario.
                    int id_Usuario = Convert.ToInt32(nuevoUsuario.Id);
                    Console.WriteLine("Para ver si la conexión sigue activa" + conexionBD);
                    //conexionBD.Close();
                    return id_Usuario;
                }
                Console.WriteLine("Para ver si la conexión sigue activa" + conexionBD);
                //conexionBD.Close();
                return 0;
            }
            Console.WriteLine("Para ver si la conexión sigue activa" + conexionBD);
            //conexionBD.Close();
            return 0;
        }


        /* Este método sirve para obtener todos los usuarios dentro la base de datos. */
        public async Task<IEnumerable<UsuarioEntitie>> ObtenerUsuarios()
        {
            try
            {
                //Crea la conexión.
                using var conexionBD = CreateConnection();
                
                //Para ejecutar el procedimiento almacenado.
                var todos_Usuarios = await conexionBD.QueryAsync<UsuarioEntitie>(
                    //Procedimiento almacenado:
                    "PROCED_ConsultarUsuarios",
                    commandType: System.Data.CommandType.StoredProcedure);

                return todos_Usuarios;
            }
            catch (SqlException error)
            {
                Console.WriteLine("No se pudo realizar correctamente la operación, esto por el siguiente error: " + error);
                return null;
            }

        }


        /* Este método sirve para obtener un usuario por medio del ID en la base de datos. 
         AUN NO SE USA*/
        public Task<UsuarioEntitie?> ObtenerUsuario_PorId(int id)
        {
            //Crea la conexión.
            using var conexionBD = CreateConnection();

            //Para ejecutar el procedimiento almacenado.
            var usuario = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                "Nombre del procedimiento almacenado",
                new {Id = id}, 
                commandType: System.Data.CommandType.StoredProcedure);
            
            return usuario;
        }

        /* Este método sirve para obtener un ID de un usuario por medio de la cédula -
         * dentro de la base de datos. */
        public bool ObtenerIDUsuario_PorCedula(string cedulaParametrizada)
        {
            //Crea la conexión.
            using var conexionBD = CreateConnection();

            //Si la cédula pasada por parametro es distinto de nulo, puede pasar.
            if(cedulaParametrizada != null)
            {                
                try
                {
                    //Para resetear la variable.
                    resultado_cedula = 0;

                    //Para ejecutar el procedimiento almacenado.
                    var usuario = conexionBD.QueryFirstOrDefaultAsync<UsuarioEntitie>(
                        //Procedimiento almacenado:
                        "PROCED_Consultar_IdUsuario_X_Cedula",
                        new { Cedula = cedulaParametrizada },                    
                        commandType: System.Data.CommandType.StoredProcedure);

                    //Para verificar lo que trae la variable: "usuario".
                    Console.WriteLine(usuario);

                    //Guarda el ID que trae el procedimiento almacenado.
                    string? usuarioId = usuario.Result?.Id.ToString();

                    /* Si el ID del usuario que se trajo en la BD es igual a nulo, puede pasar.
                     * Esto significa que en la BD no hay un usuario que exista con esa cédula -
                     * por lo que se puede enviar al método de crear usuarios. */
                    if (usuarioId == null)
                    {
                        resultado_cedula = 0;
                        return true;
                    }

                    resultado_cedula = usuario.Result!.Id;
                    return true;

                } catch (IOException error) {
                    Console.WriteLine("No se pudo realizar correctamente la operación, esto por el siguiente error: " + error);
                }
            }
            return false;
        }





//|========================================| FIN DE LA CLASE |========================================|
    }
}
