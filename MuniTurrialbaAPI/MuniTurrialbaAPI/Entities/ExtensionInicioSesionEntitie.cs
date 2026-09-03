namespace MuniTurrialbaAPI.Entities
{
    public class ExtensionInicioSesionEntitie
    {
        public string Nombre { get; set; }
        public string Apellido_1 { get; set; }
        public string Apellido_2 { get; set; }
        public string Cedula { get; set; }
        public string Correo_Electronico { get; set; }
        public string? Departamento { get; set; }
        public string Nombre_Rol { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public DateTime Fecha_Inicio_Sesion { get; set; }
        public DateTime Ultima_Conexion { get; set; }
    }
}
