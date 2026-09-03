using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IPermisoTiempoRepository
    {
        Task<bool?> CrearPermisosTiempo(PermisoTiempoCreateDto tiempodto, int idEmpleadoParametrizado);

        Task<bool?> ActualizarPermisosTiempo(PermisoTiempoCreateDto tiempodto, int idEmpleadoParametrizado);

        Task<bool?> EliminarPermisosTiempo(int idEmpleadoParametrizado);


        Task<IEnumerable<ExtensionPermisoTiempoEntitie>?> ObtenerPermisosTiempo();

        bool? VerificarPermisosTiempo_ParaCrear(int? idEmpleadoParametrizado);
        
        bool? VerificarPermisosTiempo_ParaActualizar(int? idEmpleadoParametrizado);

        bool? VerificarPermisosTiempo_ParaEliminar(int? idEmpleadoParametrizado);
    }
}
