namespace MuniTurrialbaAPI.Models
{
    public class PermisoTiempoCreateDto
    {
        /* Son los datos que serviran para -
         * la transferencia de datos. */
        public string Tipo_Permiso { get; set; }
        public string? Descripcion { get; set; }
        public DateTime Fecha_Asignacion { get; set; }
        public DateOnly Fecha_Finalizacion { get; set; }
        public bool Estado_Permiso { get; set; }
        public int Id_Empleado { get; set; }
    }
}
