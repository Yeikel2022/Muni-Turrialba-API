using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IRoleRepository
    {        
        /* Son los principales métodos que -
         * realizara el API, pero solo es  -
         * su mención. */
        Task<IEnumerable<RoleEntitie>> GetAllAsync();
        Task<RoleEntitie?> GetByIdAsync(int id);
        Task<int> CreateAsync(RoleCreateDto roledto);
    }
}
