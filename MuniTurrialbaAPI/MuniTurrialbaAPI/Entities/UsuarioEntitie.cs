namespace MuniTurrialbaAPI.Entities
{
    public class UsuarioEntitie
    {
        /* Son los datos que representan -
         * las columnas de la tabla: "Usuario" */
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido_1 { get; set; }
        public string Apellido_2 { get; set; }
        public int Edad { get; set; }
        public string Cedula { get; set; }
        public string Telefono { get; set; }
        public string Correo_Electronico { get; set; }
        public string Contraseña { get; set; }        
        public DateTime Fecha_Creacion { get; set; }
        public byte[]? Imagen_Perfil { get; set; }
        public int Id_Rol { get; set; }
    }
}
