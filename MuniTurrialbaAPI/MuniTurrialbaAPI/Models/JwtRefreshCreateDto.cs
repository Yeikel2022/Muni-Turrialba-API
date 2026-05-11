namespace MuniTurrialbaAPI.Models
{
    public class JwtRefreshCreateDto
    {
        /* Son los datos que serviran para -
         * la transferencia de datos. */
        public string TokenAcceso { get; set; }
        public string? TokenRefrescado { get; set; }
    }
}
