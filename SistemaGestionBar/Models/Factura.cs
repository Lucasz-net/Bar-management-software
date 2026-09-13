using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionBar.Models
{
    /// <summary>
    /// EXTENSIÓN AL DER. Comprobante de una venta. Se emite solo al confirmarla.
    ///
    /// Guarda los importes y los nombres COPIADOS, no resueltos por clave foránea:
    /// un comprobante emitido no puede cambiar porque después se renombre un producto
    /// o se actualice un precio. Por eso duplica datos a propósito.
    /// </summary>
    public class Factura : EntidadAuditable
    {
        public int IdFactura { get; set; }
        public int IdVenta { get; set; }

        /// <summary>Número del comprobante con el formato F-0001.</summary>
        public string Numero { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; }
        public decimal Importe { get; set; }

        /// <summary>Nombre del cliente tal como estaba el día de la emisión.</summary>
        public string? ClienteNombre { get; set; }

        /// <summary>Quién cobró, copiado igual que el nombre del cliente.</summary>
        public string? CajeroNombre { get; set; }

        public List<FacturaLinea> Lineas { get; set; } = new();

        // Navegación
        public Venta? Venta { get; set; }

        /// <summary>Suma de los renglones. Tiene que coincidir con <see cref="Importe"/>.</summary>
        public decimal TotalCalculado => Lineas.Sum(l => l.Subtotal);

        public int CantidadRenglones => Lineas.Count;
    }
}
