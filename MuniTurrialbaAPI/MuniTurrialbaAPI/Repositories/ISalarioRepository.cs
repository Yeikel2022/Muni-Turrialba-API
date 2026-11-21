using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface ISalarioRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> CrearSalarios(SalarioCreateDto salarioDto, int idEmpleadoParametrizado);
        Task<bool?> ActualizarSalarios(SalarioCreateDto salarioDto, int idEmpleadoParametrizado);
        Task<bool?> EliminarSalarios(int idEmpleadoParametrizado);


        Task<IEnumerable<ExtensionSalarioEntitie>?> ObtenerSalarios();


        bool? VerificarSalario_ParaCrear(int idEmpleadoParametrizado);
        bool? VerificarSalario_ParaActualizar(int idEmpleadoParametrizado);
        bool? VerificarSalario_ParaEliminar(int idEmpleadoParametrizado);
    }
}
