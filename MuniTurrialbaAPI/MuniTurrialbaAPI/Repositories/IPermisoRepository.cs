using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IPermisoRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> ActualizarPermisos_Usuario(PermisoCreateDto permisoDto, int idUsuarioParametrizado);


        Task<PermisoEntitie?>? ObtenerPermisos_Usuario(int idUsuarioParametrizado);


        bool? VerificarPermisosUsuario_ParaActualizar(int idUsuarioParametrizado);
    }
}
