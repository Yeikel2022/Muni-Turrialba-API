namespace MuniTurrialbaAPI.Models
{
    public class RegistroArchivoCreateDto
    {
        /* Son los datos que serviran para -
         * la transferencia de datos. */
        public string Archivo { get; set; }
        public string Nombre_Archivo { get; set; }
        public string Formato_Archivo { get; set; }
        public string Fecha_Subida { get; set; }
        public string? Fecha_Actualizacion { get; set; }
        public string? Fecha_Envio { get; set; }
        public string Nombre_Originario { get; set; }
        public string Apellido_1_Originario { get; set; }
        public string Apellido_2_Originario { get; set; }
        public string Correo_Electronico_Originario { get; set; }
        public string Departamento_Originario { get; set; }
        public string Nombre_Destinatario { get; set; }
        public string Apellido_1_Destinatario { get; set; }
        public string Apellido_2_Destinatario { get; set; }
        public string Correo_Electronico_Destinatario { get; set; }
        public string Departamento_Destinatario { get; set; }
        public string Estado_Archivo { get; set; }
    }
}
