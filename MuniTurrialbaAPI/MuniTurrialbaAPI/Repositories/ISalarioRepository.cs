using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface ISalarioRepository
    {
        Task<bool?> CrearSalarios(SalarioCreateDto salariodto, int idEmpleadoParametrizado);

        Task<bool?> ActualizarSalarios(SalarioCreateDto salariodto, int idEmpleadoParametrizado);

        Task<bool?> EliminarSalarios(int idEmpleadoParametrizado);


        Task<IEnumerable<ExtensionSalarioEntitie>?> ObtenerSalarios();

        bool? VerificarSalario_ParaCrear(int? idEmpleadoParametrizado);

        bool? VerificarSalario_ParaActualizar(int? idEmpleadoParametrizado);

        bool? VerificarSalario_ParaEliminar(int? idEmpleadoParametrizado);
    }
}
