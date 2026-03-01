namespace MuniTurrialbaAPI.Entities
{
    public class PermisoEntitie
    {        
        /* Son los datos que representan -
         * las columnas de la tabla: "Permisos" */
        public int Id { get; set; }
        public bool Leer { get; set; }
        public bool Crear { get; set; }
        public bool Actualizar { get; set; }
        public bool Eliminar { get; set; }
        public int Id_Usuario { get; set; }
    }
}
