using Microsoft.Data.SqlClient;
using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        /*
         * |==============| Zona de conexión a la BD |==============|  
         */
        //Es para el método con la asignación: 1.1
        private readonly string _connectionString;
        
        /* Asignación: 1.1
         * Esto es para definir la conexión, osea, basicamente trae la conexión que se -
         * hizo en el appsettings, y luego lo llama para que se haga dicha conexión en -
         * este lugar. */
        public RoleRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los demás métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);
        
    //|========================================================================================|

        public Task<int> CreateAsync(RoleCreateDto roledto)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RoleEntitie>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RoleEntitie?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
