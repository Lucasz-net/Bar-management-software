using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Comprobante de una venta. NO es una entidad de EF Core ni tiene tabla propia:
    /// se arma en memoria a partir de Venta/VentaDetalle cada vez que se pide, en
    /// <see cref="Data.RepositorioSql"/>.
    ///
    /// Los importes y los nombres se copian igual que antes (no se resuelven por clave
    /// foránea al vuelo desde otro lado), pero la copia ahora se hace en el momento de
    /// leer, no al emitir: si después se renombra un producto o un cliente, el
    /// comprobante de una venta vieja va a reflejar ese cambio. Es la contrapartida de
    /// no tener tablas propias para este dato.
    /// </summary>
    public class Factura
    {
        public int IdVenta { get; set; }

        /// <summary>Número del comprobante con el formato F-0001. Se deriva de IdVenta.</summary>
        public string Numero { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; }
        public decimal Importe { get; set; }

        /// <summary>Nombre del cliente al momento de leer el comprobante.</summary>
        public string? ClienteNombre { get; set; }

        /// <summary>Quién cobró, igual que el nombre del cliente.</summary>
        public string? CajeroNombre { get; set; }

        public List<FacturaLinea> Lineas { get; set; } = new();

        // Navegación
        public Venta? Venta { get; set; }

        /// <summary>Suma de los renglones. Tiene que coincidir con <see cref="Importe"/>.</summary>
        public decimal TotalCalculado => Lineas.Sum(l => l.Subtotal);

        public int CantidadRenglones => Lineas.Count;
    }
}
