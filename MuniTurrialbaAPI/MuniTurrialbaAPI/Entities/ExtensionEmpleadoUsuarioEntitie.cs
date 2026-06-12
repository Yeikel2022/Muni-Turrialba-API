namespace MuniTurrialbaAPI.Entities
{
    public class ExtensionEmpleadoUsuarioEntitie
    {
        public string Nombre_Empleado { get; set; }
        public string Apellido_1_Empleado { get; set; }
        public string Apellido_2_Empleado { get; set; }
        public int Edad_Empleado { get; set; }
        public string Cedula_Empleado { get; set; }
        public string Telefono_Empleado { get; set; }
        public string Correo_Electronico_Empleado { get; set; }
        public string Contraseña_Empleado { get; set; }
        public string Nombre_Rol { get; set; }
        public DateTime Fecha_Creacion_Empleado { get; set; }
        public string? Departamento { get; set; }
        public bool Activo { get; set; }

    }
}
