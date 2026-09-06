using System;

namespace SistemaGestionBar.Models
{
    /// <summary>Tabla Cliente. Extiende a Persona con los datos propios del cliente (RF-05).</summary>
    public class Cliente : EntidadAuditable
    {
        public int IdCliente { get; set; }
        public int IdPersona { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public Persona Persona { get; set; } = null!;

        /// <summary>Texto que muestra el combo de clientes del punto de venta.</summary>
        public string NombreMostrado => Persona?.Nombre ?? $"Cliente #{IdCliente}";
    }
}
