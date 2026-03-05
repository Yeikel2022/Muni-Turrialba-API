using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IUsuarioRepository
    {
        /* Son los principales métodos que -
         * realizara el API, pero solo es  -
         * su mención. */
        Task<IEnumerable<UsuarioEntitie>> ObtenerUsuarios();
        Task<UsuarioEntitie?> ObtenerUsuario_PorId(int id);
        bool ObtenerIDUsuario_PorCedula(string cedulaParametrizada);
        Task<int> CrearUsuario(UsuarioCreateDto userdto);
    }
}
