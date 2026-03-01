namespace MuniTurrialbaAPI.Entities
{
    public class FAQEntitie
    {        
        /* Son los datos que representan -
         * las columnas de la tabla: "FAQ" */
        public int Id { get; set; }
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
        public string Tipo_Prioridad { get; set; }
    }
}
