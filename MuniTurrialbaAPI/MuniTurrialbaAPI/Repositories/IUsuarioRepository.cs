using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IUsuarioRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que, cuando se usen, -
         * estos puedan aceptar valores nulos. */

        Task<bool?> CrearUsuario(UsuarioCreateDto userdto, bool tipo);

        string? CrearCodigoQR(string nombreParametrizado, string apellidosParametrizados, string correoParametrizado);

        Task<bool?> EnviarCorreo(string correoParametrizado);
        
        bool VerificarCodigo(string codigoParametrizado);

        Task<bool> ActualizarContraseñaUsuario(string contraseñaParametrizado, string correoParametrizado);

        Task<bool?> ActualizarFotoPerfil(string fotoParametrizada, string correoParametrizado);

        Task<bool?> ActualizarUsuario(UsuarioCreateDto userdto, string cedulaParametrizada);
        
        Task<bool?> EliminarUsuario(int idUsuarioParametrizado);

        Task<UsuarioEntitie?>? VerificarUsuario(string correoParametrizado, string contraseñaParametrizado);


        Task<IEnumerable<UsuarioEntitie>?> ObtenerUsuarios();

        Task<UsuarioEntitie?>? ObtenerUsuario_PorCorreo(string correoParametrizado);
        
        Task<UsuarioEntitie?>? ObtenerUsuario_PorCedula(string cedulaParametrizada);

        bool ObtenerIDUsuario_PorCedula(string cedulaParametrizada);

        bool ObtenerIDUsuario_PorCorreo(string correoParametrizado);    

        bool ValidarUsuario_PorCorreo(string correoParametrizado);

        bool? VerificarUsuario_ParaActualizar(string cedulaParametrizada);

    }
}
