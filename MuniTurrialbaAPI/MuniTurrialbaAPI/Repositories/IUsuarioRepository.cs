using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IUsuarioRepository
    {
        /* Son los principales métodos que -
         * realizara el API, pero solo es  -
         * su mención. */
        Task<IEnumerable<UsuarioEntitie>> GetAllAsync();
        Task<UsuarioEntitie?> GetByIdAsync(int id);
        Task<int> CreateAsync(UsuarioCreateDto userdto);
    }
}
