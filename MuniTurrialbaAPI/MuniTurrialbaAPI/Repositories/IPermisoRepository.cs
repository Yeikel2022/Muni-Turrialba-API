using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IPermisoRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que, cuando se usen, -
         * estos puedan aceptar valores nulos. */

        //Task<bool?> CrearPermiso_Usuario(PermisoCreateDto permisodto);

        Task<PermisoEntitie?>? ObtenerPermisosUsuario(int idUsuarioParametrizado);


    }
}
