namespace MuniTurrialbaAPI.Entities
{
    public class EmpleadoEntitie
    {
        /* Son los datos que representan -
         * las columnas de la tabla: "Empleado" */
        public int Id { get; set; }
        public bool Activo { get; set; }
        public string? Departamento { get; set; }
        public int Id_Usuario { get; set; }
        public int Id_Permiso_Tiempo { get; set; }
        public int Id_Salario { get; set; }        
    }
}
