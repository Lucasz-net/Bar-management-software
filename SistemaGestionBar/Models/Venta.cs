using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionBar.Models
{
    public class Venta : EntidadAuditable
    {
        public int IdVenta { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public int IdMetodoPago { get; set; }
        public EstadoVenta EstadoVenta { get; set; } = EstadoVenta.Pendiente;
        public int IdCliente { get; set; }

        public int IdCajero { get; set; }

        /// <summary>Null en barra y para llevar.</summary>
        public int? IdMesero { get; set; }

        public int? IdUbicacion { get; set; }

        public ModalidadConsumo ModalidadConsumo { get; set; } = ModalidadConsumo.Local;

        public MetodoPago MetodoPago { get; set; } = null!;
        public Cliente Cliente { get; set; } = null!;
        public Usuario Cajero { get; set; } = null!;
        public Usuario? Mesero { get; set; }
        public Ubicacion? Ubicacion { get; set; }
        public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();

        /// <summary>No se persiste: se suma del detalle.</summary>
        public decimal Total => Detalles.Sum(d => d.Subtotal);
    }
}
