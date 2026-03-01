namespace MuniTurrialbaAPI.Entities
{
    public class RegistroArchivoEntitie
    {        
        /* Son los datos que representan -
         * las columnas de la tabla: "Registro_Archivo" */
        public int Id { get; set; }
        public byte Archivo { get; set; }
        public string Formato_Archivo { get; set; }
        public DateTime Fecha_Subida { get; set; }
        public DateTime Fecha_Actualizacion { get; set; }
        public string Nombre_Originario { get; set; }
        public string Apellido_1_Originario { get; set; }
        public string Apellido_2_Originario { get; set; }
        public string Departamento_Originario { get; set; }
        public string Correo_Electronico_Originario { get; set; }
        public string Nombre_Destinatario { get; set; }
        public string Apellido_1_Destinatario { get; set; }
        public string Apellido_2_Destinatario { get; set; }
        public string Departamento_Destinatario { get; set; }                
        public string Correo_Electronico_Destinatario { get; set; }
        public bool Estado_Archivo { get; set; }
    }
}
