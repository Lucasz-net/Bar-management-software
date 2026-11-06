using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionBar.Models
{
    /// <summary>Comprobante armado en memoria al leer; no tiene tabla propia.</summary>
    public class Factura
    {
        public int IdVenta { get; set; }

        public string Numero { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; }
        public decimal Importe { get; set; }

        public string? ClienteNombre { get; set; }

        public string? CajeroNombre { get; set; }

        public string? MetodoPagoNombre { get; set; }

        public ModalidadConsumo Modalidad { get; set; } = ModalidadConsumo.Local;

        public string? UbicacionNombre { get; set; }

        public string? MeseroNombre { get; set; }

        public List<FacturaLinea> Lineas { get; set; } = new();

        public Venta? Venta { get; set; }

        public string DestinoTexto =>
            Modalidad == ModalidadConsumo.ParaLlevar
                ? "Para llevar"
                : UbicacionNombre ?? "En el local";

        public bool TieneMesero => !string.IsNullOrWhiteSpace(MeseroNombre);

        public decimal TotalCalculado => Lineas.Sum(l => l.Subtotal);

        public int CantidadRenglones => Lineas.Count;
    }
}
