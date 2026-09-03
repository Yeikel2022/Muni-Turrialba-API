namespace MuniTurrialbaAPI.Entities
{
    public class ExtensionPermisoTiempoEntitie
    {
        public string Nombre { get; set; }
        public string Apellido_1 { get; set; }
        public string Apellido_2 { get; set; }
        public string Cedula { get; set; }
        public string? Departamento { get; set; }
        public string Tipo_Permiso { get; set; }
        public string? Descripcion { get; set; }
        public DateTime Fecha_Asignacion { get; set; }
        public DateTime Fecha_Finalizacion { get; set; }
    }
}

