using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IEmpleadoRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> CrearEmpleado(EmpleadoCreateDto empleadoDto, int idUsuarioParametrizado);
        Task<bool?> ActualizarEmpleado(EmpleadoCreateDto empleadoDto, int idUsuarioParametrizado);
        Task<bool?> EliminarEmpleado(int idUsuarioParametrizado);


        Task<IEnumerable<ExtensionEmpleadoUsuarioEntitie>?> ObtenerEmpleados();
        Task<EmpleadoEntitie?>? ObtenerEmpleado_PorIdUsuario(int idUsuarioParametrizado);


        bool? VerificarEmpleado_ParaCrear(int idUsuarioParametrizado);
        bool? VerificarEmpleado_ParaActualizar(int idUsuarioParametrizado);
        bool? VerificarEmpleado_ParaEliminar(int idUsuarioParametrizado);
    }
}
