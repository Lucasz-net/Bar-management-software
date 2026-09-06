using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Venta (cabecera del ticket).
    /// El total NO se persiste: se calcula sumando los subtotales del detalle (regla de negocio).
    /// IdUbicacion queda en null cuando la modalidad es "Para Llevar" (RF-03).
    /// </summary>
    public class Venta : EntidadAuditable
    {
        public int IdVenta { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public int IdMetodoPago { get; set; }
        public EstadoVenta EstadoVenta { get; set; } = EstadoVenta.Pendiente;
        public int IdCliente { get; set; }

        /// <summary>RF-02: usuario que procesa el cobro. Obligatorio.</summary>
        public int IdCajero { get; set; }

        /// <summary>RF-02: quien tomó el pedido. Obligatorio en consumo local, null si es para llevar.</summary>
        public int? IdMesero { get; set; }

        /// <summary>RF-03: null cuando ModalidadConsumo es ParaLlevar.</summary>
        public int? IdUbicacion { get; set; }

        public ModalidadConsumo ModalidadConsumo { get; set; } = ModalidadConsumo.Local;

        // Navegación
        public MetodoPago MetodoPago { get; set; } = null!;
        public Cliente Cliente { get; set; } = null!;
        public Usuario Cajero { get; set; } = null!;
        public Usuario? Mesero { get; set; }
        public Ubicacion? Ubicacion { get; set; }
        public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();

        /// <summary>RF-01: total dinámico, sumatoria de los subtotales del detalle.</summary>
        public decimal Total => Detalles.Sum(d => d.Subtotal);
    }
}
