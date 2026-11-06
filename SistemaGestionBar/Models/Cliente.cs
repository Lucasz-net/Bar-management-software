using System;

namespace SistemaGestionBar.Models
{
    public class Cliente : EntidadAuditable
    {
        public int IdCliente { get; set; }
        public int IdPersona { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public Persona Persona { get; set; } = null!;

        public string NombreMostrado => Persona?.NombreCompleto ?? $"Cliente #{IdCliente}";
    }
}
