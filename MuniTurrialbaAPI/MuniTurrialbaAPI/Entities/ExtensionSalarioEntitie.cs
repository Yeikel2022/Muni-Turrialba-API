namespace MuniTurrialbaAPI.Entities
{
    public class ExtensionSalarioEntitie
    {
        /* Son los datos que representan -
         * las columnas de la tabla: "Salario". 
         * 
         * Nota: Esto es para mostrar toda la -
         * información de los salarios. */
        public string Nombre { get; set; }
        public string Apellido_1 { get; set; }
        public string Apellido_2 { get; set; }
        public int edad { get; set; }
        public string Cedula { get; set; }
        public string? Departamento { get; set; }
        public DateTime Fecha_Entrega { get; set; }
        public decimal Salario { get; set; }
        public string? Descripcion { get; set; }
    }
}
