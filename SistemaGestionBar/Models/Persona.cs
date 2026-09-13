namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Persona. Centraliza los datos personales de empleados y clientes (RF-10),
    /// evitando duplicar nombre/DNI/teléfono/email en cada tabla que los necesite.
    ///
    /// El correo de esta tabla es el CONTACTO PERSONAL. El correo con el que un empleado
    /// inicia sesión es otro y vive en Usuario: una misma persona puede ser cliente del bar
    /// con su correo particular y empleada con su correo de trabajo.
    /// </summary>
    public class Persona : EntidadAuditable
    {
        public int IdPersona { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// EXTENSIÓN AL DER. Nombre y apellido separados: es lo que permite ordenar y
        /// buscar el padrón por apellido, que es como se busca a una persona en la vida real.
        /// </summary>
        public string Apellido { get; set; } = string.Empty;

        public string? DniCuit { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }

        // Navegación
        public Usuario? Usuario { get; set; }
        public Cliente? Cliente { get; set; }

        /// <summary>Para mostrar: "Nombre Apellido". Único lugar donde se arma el nombre.</summary>
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

        /// <summary>Para listados ordenados por apellido: "Apellido, Nombre".</summary>
        public string NombreParaListado =>
            string.IsNullOrWhiteSpace(Apellido) ? Nombre : $"{Apellido}, {Nombre}";

        /// <summary>Tiene acceso al sistema: es personal del bar.</summary>
        public bool EsEmpleado => Usuario is not null;

        /// <summary>Está registrada como cliente del bar (RF-05).</summary>
        public bool EsCliente => Cliente is not null;
    }
}
