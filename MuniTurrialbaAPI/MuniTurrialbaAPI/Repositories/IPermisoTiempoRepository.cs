using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IPermisoTiempoRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> CrearPermisosTiempo(PermisoTiempoCreateDto tiempoDto, int idEmpleadoParametrizado);
        Task<bool?> ActualizarPermisosTiempo(PermisoTiempoCreateDto tiempoDto, int idEmpleadoParametrizado);
        Task<bool?> EliminarPermisosTiempo(int idEmpleadoParametrizado);


        Task<IEnumerable<ExtensionPermisoTiempoEntitie>?> ObtenerPermisosTiempo();


        bool? VerificarPermisosTiempo_ParaCrear(int idEmpleadoParametrizado);
        bool? VerificarPermisosTiempo_ParaActualizar(int idEmpleadoParametrizado);
        bool? VerificarPermisosTiempo_ParaEliminar(int idEmpleadoParametrizado);
    }
}
