using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IUsuarioRepository
    {
        /* Son los principales métodos que -
         * realizara el API, pero solo es  -
         * su mención. */
        
        /* NOTA: Se le coloco el: ?, para que, cuando se use el método -
         * este pueda aceptar valores nulos respectivamente.*/
        Task<IEnumerable<UsuarioEntitie>?> ObtenerUsuarios();

        /* NOTA: Se le coloco el: ?, para que, cuando se use el método -
         * este pueda aceptar valores nulos respectivamente.*/
        Task<UsuarioEntitie?>? ObtenerUsuario_PorCorreo(string correoElectronico);
        
        bool ObtenerIDUsuario_PorCedula(string cedulaParametrizada);
        
        Task<int> CrearUsuario(UsuarioCreateDto userdto);
    }
}
