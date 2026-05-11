using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MuniTurrialbaAPI.Repositories
{
    public class JwtRepository : IJwtRepository
    {
        //          |==============| Zona de configuración |==============|

        //Es para el método con la asignación: [1.1]
        private readonly IConfiguration _configuration;


        /* Asignación: [1.1]
         * Esto es para definir la configuración, osea, basicamente trae la configuración -
         * que se hizo en el appsettings, y luego lo llama para que se pueda acceder en -
         * este lugar. */
        public JwtRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        //         |==============| Zona de los métodos  |==============|


        /* Este método sirve para crear un token al usuario. */
        public string crearTokenJWT(IEnumerable<Claim> claimsDelToken)
        {
            //Esto es para poder encriptar la llave del token:
            var llaveToken = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            //Aqui se termina de encriptar el token:
            var credenciales = new SigningCredentials(llaveToken, SecurityAlgorithms.HmacSha256);

            //Aqui se crea el token para el usuario:
            var TokenGenerado = new JwtSecurityToken(
                claims: claimsDelToken,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: credenciales);

            /* Se construye el token con los datos que se pasaron por parametro -
             * y las credenciales generadas anteriormente. */
            return new JwtSecurityTokenHandler().WriteToken(TokenGenerado);
        }


        /* Este método sirve para validar el token que esta pasando el usuario. */
        public ClaimsPrincipal? validarTokenJWT(string tokenUsuario)
        {
            /* Se crea un JwtSecurityTokenHandler para poder comparar el token -
             * que paso el usuario por el parámetro, entre los datos que se tienen -
             * sobre la configuración del token. */
            var tokenHandler = new JwtSecurityTokenHandler();
            
            //Se trae y se codifica nuevamente la llave de la configuración:
            var llaveSistema = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            try
            {
                /* Aquí se realiza la comparación entre el token que se paso, -
                 * entre los datos del sistema, osea los del token respectivamente. */
                return tokenHandler.ValidateToken(tokenUsuario, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true, 
                    ValidateIssuerSigningKey = true, 
                    IssuerSigningKey = new SymmetricSecurityKey(llaveSistema),
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch (Exception error)
            {
                Debug.WriteLine("No se pudo realizar correctamente la operación, " +
                    "esto por el siguiente error: " + error);
                return null;
            }
        }


        /* Este método sirve para poder refrescar el token del usuario. */
        public string refrescarTokenJWT()
        {
            var IdTokenRefrescado = Guid.NewGuid().ToString();
            return IdTokenRefrescado;
        }


        //|========================================| FIN DE LA CLASE |========================================|
    }
}
