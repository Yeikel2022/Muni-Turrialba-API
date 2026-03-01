namespace MuniTurrialbaAPI.Entities
{
    public class HistoricoEntitie
    {        
        /* Son los datos que representan -
         * las columnas de la tabla: "Historico" */
        public int Id { get; set; }
        public string Nombre_Usuario { get; set; }
        public string Accion { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha_Realizada { get; set; }
    }
}
