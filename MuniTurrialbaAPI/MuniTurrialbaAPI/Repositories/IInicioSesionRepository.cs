using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IInicioSesionRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> CrearRegistroInicioSesion(InicioSesionCreateDto sesionDto, int idUsuarioParametrizado);

        Task<IEnumerable<ExtensionInicioSesionEntitie>?> ObtenerRegistros_InicioSesion();

        bool? VerificarUsuario_ParaCrear(int idUsuarioParametrizado);
    }
}
