using System.Collections.ObjectModel;
using System.Linq;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Registro de ventas: historial de cabeceras con su detalle.
    /// Es solo lectura a propósito. Una venta ya cobrada no se edita: si algo salió mal
    /// se anula y se hace de nuevo, para que el histórico y el stock sigan cuadrando.
    /// </summary>
    public class AdminVentasViewModel : SeccionAdminViewModel
    {
        public AdminVentasViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, "Ventas", "ReceiptTextOutline",
                   "Historial de ventas con su detalle, cajero, mesero y ubicación")
        {
            Ventas = new ObservableCollection<Venta>();
            Detalle = new ObservableCollection<VentaDetalle>();
            Recargar();
        }

        public ObservableCollection<Venta> Ventas { get; }
        public ObservableCollection<VentaDetalle> Detalle { get; }

        private Venta? _seleccionada;
        public Venta? Seleccionada
        {
            get => _seleccionada;
            set
            {
                if (!SetProperty(ref _seleccionada, value))
                    return;

                Detalle.Clear();
                if (value is not null)
                {
                    foreach (var d in value.Detalles)
                        Detalle.Add(d);
                }

                OnPropertyChanged(nameof(HaySeleccion));
                OnPropertyChanged(nameof(TituloDetalle));
            }
        }

        public bool HaySeleccion => Seleccionada is not null;

        public string TituloDetalle => Seleccionada is null
            ? "Seleccioná una venta para ver el detalle"
            : $"Detalle de la venta #{Seleccionada.IdVenta}";

        private int _cantidadVentas;
        public int CantidadVentas
        {
            get => _cantidadVentas;
            private set => SetProperty(ref _cantidadVentas, value);
        }

        private decimal _totalFacturado;
        public decimal TotalFacturado
        {
            get => _totalFacturado;
            private set => SetProperty(ref _totalFacturado, value);
        }

        private decimal _ticketPromedio;
        public decimal TicketPromedio
        {
            get => _ticketPromedio;
            private set => SetProperty(ref _ticketPromedio, value);
        }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionada?.IdVenta;

            Ventas.Clear();
            foreach (var v in Repositorio.ObtenerVentas().OrderByDescending(v => v.IdVenta))
                Ventas.Add(v);

            CantidadVentas = Ventas.Count;
            TotalFacturado = Ventas.Sum(v => v.Total);
            TicketPromedio = CantidadVentas == 0 ? 0 : TotalFacturado / CantidadVentas;

            Seleccionada = Ventas.FirstOrDefault(v => v.IdVenta == idPrevio) ?? Ventas.FirstOrDefault();
        }
    }
}
