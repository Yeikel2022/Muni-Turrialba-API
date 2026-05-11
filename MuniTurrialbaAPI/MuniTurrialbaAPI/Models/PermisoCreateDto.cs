namespace MuniTurrialbaAPI.Models
{
    public class PermisoCreateDto
    {
        /* Son los datos que serviran para -
         * la transferencia de datos. */
        public bool Leer { get; set; }
        public bool Crear { get; set; }
        public bool Actualizar { get; set; }
        public bool Eliminar { get; set; }
        public int Id_Usuario { get; set; }
    }
}
