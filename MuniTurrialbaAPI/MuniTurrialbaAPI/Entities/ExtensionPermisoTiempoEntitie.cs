namespace MuniTurrialbaAPI.Entities
{
    public class ExtensionPermisoTiempoEntitie
    {
        /* Son los datos que representan -
         * las columnas de la tabla: "Permiso_Tiempo". 
         * 
         * Nota: Esto es para mostrar toda la -
         * información de los permisos de tiempo. */
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

