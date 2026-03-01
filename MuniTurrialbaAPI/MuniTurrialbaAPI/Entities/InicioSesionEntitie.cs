namespace MuniTurrialbaAPI.Entities
{
    public class InicioSesionEntitie
    {        
        /* Son los datos que representan -
         * las columnas de la tabla: "Inicio_Sesion" */
        public int Id { get; set; }
        public DateOnly Fecha_Inicio_Sesion {  get; set; }
        public TimeOnly Hora {  get; set; }
        public DateTime Ultima_Conexion {  get; set; }
        public int Id_Usuario {  get; set; }
    }
}
