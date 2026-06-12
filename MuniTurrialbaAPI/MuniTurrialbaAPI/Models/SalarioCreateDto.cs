namespace MuniTurrialbaAPI.Models
{
    public class SalarioCreateDto
    {
        /* Son los datos que serviran para -
         * la transferencia de datos. */
        public DateTime Fecha_Entrega { get; set; }
        public decimal Salario { get; set; }
        public string? Descripcion { get; set; }
        public int Id_Empleado { get; set; }
    }
}
