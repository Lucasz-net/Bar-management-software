namespace SistemaGestionBar.Models
{
    public class Persona : EntidadAuditable
    {
        public int IdPersona { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        /// <summary>DNI sin separadores. La columna conserva el nombre dni_cuit.</summary>
        public string? DniCuit { get; set; }
        public string? Telefono { get; set; }
        /// <summary>Correo personal; el de acceso al sistema está en Usuario.</summary>
        public string? Email { get; set; }

        public Usuario? Usuario { get; set; }
        public Cliente? Cliente { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

        public string DniFormateado => Documento.Formatear(DniCuit);

        public string NombreParaListado =>
            string.IsNullOrWhiteSpace(Apellido) ? Nombre : $"{Apellido}, {Nombre}";

        public bool EsEmpleado => Usuario is not null;

        public bool EsCliente => Cliente is not null;
    }
}
