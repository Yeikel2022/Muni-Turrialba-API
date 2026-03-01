namespace MuniTurrialbaAPI.Entities
{
    public class PermisoTiempoEntitie
    {        
        /* Son los datos que representan -
         * las columnas de la tabla: "Permiso_Tiempo" */
        public int Id { get; set; }
        public string Tipo_Permiso { get; set; }
        public string? Descripcion { get; set; }
        public DateTime Fecha_Asignacion { get; set; }
        public DateOnly Fecha_Finalizacion { get; set; }
        public bool Estado_Permiso { get; set; }
    }
}
