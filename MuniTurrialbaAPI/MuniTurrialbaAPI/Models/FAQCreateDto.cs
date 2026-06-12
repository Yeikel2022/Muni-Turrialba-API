namespace MuniTurrialbaAPI.Models
{
    public class FAQCreateDto
    {
        /* Son los datos que serviran para -
         * la transferencia de datos. */
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
        public string Tipo_Prioridad { get; set; }
    }
}
