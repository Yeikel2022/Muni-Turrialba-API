using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IRegistroArchivoRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> CrearRegistrosArchivos(RegistroArchivoCreateDto archivoDto, int idEmpleadoParametrizado);
        Task<bool?> ActualizarRegistrosArchivos(RegistroArchivoCreateDto archivoDto, int idEmpleadoActualParametrizado, int idEmpleadoNuevoParametrizado);
        Task<bool?> ActualizarEstadoRegistroArchivo(string estadoArchivoParametrizado, string fechaSubidaParametrizada, int idEmpleadoParametrizado);
        Task<bool?> EliminarRegistrosArchivos(string fechaSubidaParametrizada, int idEmpleadoParametrizado);
        Task<bool?> CompartirRegistrosArchivos(RegistroArchivoCreateDto archivoDto, int idEmpleadoParametrizado);


        Task<IEnumerable<RegistroArchivoEntitie>?> ObtenerRegistrosArchivos();
        Task<IEnumerable<RegistroArchivoEntitie>?> ObtenerRegistrosArchivosCompartidos(int idEmpleadoParametrizado);


        bool? VerificarRegistrosArchivo_ParaActualizar(int idEmpleadoParametrizado, string fechaSubidaParametrizada);
        bool? VerificarRegistrosArchivo_ParaEliminar(int idEmpleadoParametrizado, string fechaSubidaParametrizada);
        bool? VerificarRegistrosArchivo_ParaCompartir(int idEmpleadoParametrizado, string fechaSubidaParametrizada);
    }
}
