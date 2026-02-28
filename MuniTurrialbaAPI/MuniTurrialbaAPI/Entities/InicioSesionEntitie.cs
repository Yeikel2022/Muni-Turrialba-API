namespace MuniTurrialbaAPI.Entities
{
    public class InicioSesionEntitie
    {
        public DateOnly Fecha_Inicio_Sesion {  get; set; }
        public TimeOnly Hora {  get; set; }
        public DateTime Ultima_Conexion {  get; set; }
        public int Id_Usuario {  get; set; }
    }
}
