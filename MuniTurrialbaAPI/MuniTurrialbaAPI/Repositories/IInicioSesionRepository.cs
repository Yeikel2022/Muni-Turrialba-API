using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IInicioSesionRepository
    {
        Task<bool?> CrearRegistroInicioSesion(InicioSesionCreateDto sesiondto, int idUsuarioParametrizado);


        Task<IEnumerable<ExtensionInicioSesionEntitie>?> ObtenerRegistros_InicioSesion();
        bool? VerificarUsuario_ParaCrear(int? idUsuarioParametrizado);
    }
}
