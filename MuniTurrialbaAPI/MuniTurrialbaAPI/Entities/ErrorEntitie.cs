namespace MuniTurrialbaAPI.Entities
{
    public class ErrorEntitie
    {
        /* Son los datos que representan -
         * las columnas de la tabla: "Errores" */
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public DateTime Fecha_Ocurrido { get; set; }
    }
}
