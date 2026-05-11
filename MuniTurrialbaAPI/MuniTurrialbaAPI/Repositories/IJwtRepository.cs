using MuniTurrialbaAPI.Models;
using System.Security.Claims;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IJwtRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que, cuando se usen, -
         * estos puedan aceptar valores nulos. */

        string crearTokenJWT(IEnumerable<Claim> claimsDelToken);
        
        ClaimsPrincipal? validarTokenJWT(string tokenUsuario);
        
        string refrescarTokenJWT();
        
        /* NOTA: 
         *  Como tal, falta por hacer el eliminar y refrescar el token -
         *  del usuario, entonces, cuando se tenga el chance, realizar -
         *  dicha actualización. */
        
    }
}
