namespace MuniTurrialbaAPI.Entities
{
    public class SalarioEntitie
    {
        /* Son los datos que representan -
         * las columnas de la tabla: "Salario" */
        public int Id { get; set; }
        public DateTime Fecha_Entrega { get; set; }
        public decimal Salario { get; set; }
        public string? Descripcion { get; set; }
    }
}
