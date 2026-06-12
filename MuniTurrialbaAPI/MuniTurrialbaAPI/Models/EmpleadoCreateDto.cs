namespace MuniTurrialbaAPI.Models
{
    public class EmpleadoCreateDto
    {
        /* Son los datos que serviran para -
         * la transferencia de datos. */
        public bool Activo { get; set; }
        public string? Departamento { get; set; }
        public int Id_Usuario { get; set; }
    }
}
