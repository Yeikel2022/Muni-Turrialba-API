using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IUsuarioRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> CrearUsuario(UsuarioCreateDto usuarioDto, bool tipoParametrizado);
        Task<bool?> ActualizarUsuario(UsuarioCreateDto usuarioDto, string cedulaParametrizada);
        Task<bool?> ActualizarContraseñaUsuario(string contraseñaParametrizada, string correoParametrizado);
        Task<bool?> ActualizarFotoPerfil(string fotoParametrizado, string correoParametrizado);
        Task<bool?> EliminarUsuario(int idUsuarioParametrizado);


        bool? EnviarCorreo(string correoParametrizado);
        string? CrearCodigoQR(string nombreParametrizado, string apellidosParametrizados, string correoParametrizado);


        Task<UsuarioEntitie?>? ObtenerUsuario_PorCorreo(string correoParametrizado);
        Task<UsuarioEntitie?>? ObtenerUsuario_PorCedula(string cedulaParametrizada);


        Task<UsuarioEntitie?>? VerificarUsuario(string contraseñaParametrizada, string correoParametrizado);
        bool? VerificarUsuario_ParteUno_ParaCrear(string cedulaParametrizada);
        bool? VerificarUsuario_ParteDos_ParaCrear(string correoParametrizado);
        bool? VerificarUsuario_ParaActualizar(string cedulaParametrizada);
        bool? VerificarUsuario_ParaEliminar(int idUsuarioParametrizado);


        bool? VerificarCodigo(string codigoParametrizado);
        bool? VerificarCorreo(string correoParametrizado);
    }
}
