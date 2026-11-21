using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MuniTurrialbaAPI.Repositories
{
    public class JwtRepository : IJwtRepository
    {
        //                  |==============| Zona de conexión a la BD |==============|

        //Es para el método con la asignación: [1.1]
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;


        /* Asignación: [1.1]
         * Esto es para definir la configuración, osea, basicamente trae la configuración -
         * que se hizo en el appsettings, y luego lo llama para que se pueda acceder en -
         * este lugar. */
        public JwtRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        //Sirve para que los métodos de abajo puedan enviar y/o utilizar la BD.
        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);


        //            |==============| Zona de los métodos  |==============|

        /* Estos métodos sirven para crear, actualizar o eliminar un token al usuario(a). */
        public string? crearTokenJWT(IEnumerable<Claim> claimsDelToken)
        {
            try
            {
                //Esto es para poder encriptar la llave del token:
                var llaveToken = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

                //Aqui se termina de encriptar el token:
                var credenciales = new SigningCredentials(llaveToken, SecurityAlgorithms.HmacSha256);

                //Aqui se crea el token para el usuario(a):
                var TokenGenerado = new JwtSecurityToken(
                    claims: claimsDelToken,
                    expires: DateTime.Now.AddMinutes(75),
                    signingCredentials: credenciales);

                /* Se construye el token con los datos que se pasaron por parametro -
                 * y las credenciales generadas anteriormente. */
                return new JwtSecurityTokenHandler().WriteToken(TokenGenerado);
            }
            catch (Exception error)
            {
                string lugarOcurrido = "crearTokenJWT";

                RegistrarError(error, lugarOcurrido);

                return null;
            }
        }

        public string refrescarTokenJWT()
        {
            var IdTokenRefrescado = Guid.NewGuid().ToString();
            return IdTokenRefrescado;
        }



        /* Este método sirve para validar el token que esta pasando el usuario(a). */
        public ClaimsPrincipal? validarTokenJWT(string tokenUsuario)
        {
            /* Se crea un JwtSecurityTokenHandler para poder comparar el token -
             * que paso el usuario(a) por el parámetro, entre los datos que se -
             * tienen sobre la configuración del token. */
            var tokenHandler = new JwtSecurityTokenHandler();
            
            //Se trae y se codifica nuevamente la llave de la configuración:
            var llaveSistema = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            try
            {
                /* Aquí se realiza la comparación entre el token que se paso con los datos -
                 * del sistema, osea los del token respectivamente. */
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
                return null;
            }
        }



        /* Este método sirve para registrar los errores que podrian suceder durante la ejecución -
         * de los demás métodos respectivamente. */
        private async void RegistrarError(Exception errorParametrizado, string lugarOcurridoParametrizado)
        {
            //Crea la conexión hacia la BD.
            using var nuevaConexionBD = CreateConnection();

            //Se crea una variable de tipo datetime para indicar la fecha en que ocurrio el error.
            DateTime fechaOcurrido = DateTime.Now;

            //Para ejecutar el procedimiento almacenado.
            var registrarError = await nuevaConexionBD.ExecuteAsync("PROCED_CrearRegistroErrores",
                //Se coloca los parametros:
                new
                {
                    Nombre = errorParametrizado.GetType().ToString(),
                    Descripcion = errorParametrizado.Message,
                    Lugar_Ocurrido = lugarOcurridoParametrizado,
                    Fecha_Ocurrido = fechaOcurrido
                },
                commandType: System.Data.CommandType.StoredProcedure);

            /* Comandos para poder ver el resultado en las variables en un debug:
             * Debug.WriteLine("No se pudo realizar correctamente la operación, esto por el siguiente error: " + errorParametrizado);
             * Debug.WriteLine("Para ver si la nueva conexión sigue activa: " + nuevaConexionBD.State);
             * Debug.WriteLine("Para ver si la nueva conexión se cerro: " + nuevaConexionBD.State); */

            //Se cierra la conexión por temas de buenas prácticas.
            nuevaConexionBD.Close();
        }


        //|========================================| FIN DE LA CLASE |========================================|
    }
}
