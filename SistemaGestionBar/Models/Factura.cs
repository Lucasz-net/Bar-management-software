using System;
using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    public class Factura
    {
        public int IdFactura { get; set; }
        public int IdVenta { get; set; }
        public string Numero { get; set; } = string.Empty; // serie/número
        public DateTime FechaEmision { get; set; }
        public decimal Importe { get; set; }

        // Datos del cliente para consulta rápida
        public string? ClienteNombre { get; set; }

        public List<FacturaLinea> Lineas { get; set; } = new();
    }
}
