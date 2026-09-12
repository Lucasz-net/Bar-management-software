using System.Collections.ObjectModel;
using System.Linq;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;
using System.ComponentModel;
using System.Windows.Data;

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
            VerFacturaCommand = new RelayCommand(VerFactura, () => Seleccionada is not null);
            Recargar();
        }

        public ObservableCollection<Venta> Ventas { get; }
        public ObservableCollection<VentaDetalle> Detalle { get; }
        private ICollectionView? _ventasView;
        public ICollectionView? VentasView => _ventasView;

        private string _filtro = string.Empty;
        public string Filtro
        {
            get => _filtro;
            set
            {
                if (SetProperty(ref _filtro, value))
                {
                    _ventasView?.Refresh();
                }
            }
        }

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
                // Forzar reevaluación de CanExecute en los comandos (actualiza botones/menús)
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
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

        public System.Windows.Input.ICommand? VerFacturaCommand { get; }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionada?.IdVenta;

            Ventas.Clear();
            foreach (var v in Repositorio.ObtenerVentas().OrderByDescending(v => v.IdVenta))
                Ventas.Add(v);

            // Inicializar / refrescar la vista filtrada
            if (_ventasView is null)
            {
                _ventasView = CollectionViewSource.GetDefaultView(Ventas);
                _ventasView.Filter = o => FiltrarVenta(o as Venta);
            }
            else
            {
                _ventasView.Refresh();
            }

            CantidadVentas = Ventas.Count;
            TotalFacturado = Ventas.Sum(v => v.Total);
            TicketPromedio = CantidadVentas == 0 ? 0 : TotalFacturado / CantidadVentas;

            Seleccionada = Ventas.FirstOrDefault(v => v.IdVenta == idPrevio) ?? Ventas.FirstOrDefault();
        }

        private bool FiltrarVenta(Venta? v)
        {
            if (v is null)
                return false;

            if (string.IsNullOrWhiteSpace(Filtro))
                return true;

            var q = Filtro.Trim();
            if (v.IdVenta.ToString().Contains(q))
                return true;

            if (!string.IsNullOrWhiteSpace(v.Cliente?.NombreMostrado) && v.Cliente.NombreMostrado.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(v.Cajero?.NombreCompleto) && v.Cajero.NombreCompleto.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(v.Mesero?.NombreCompleto) && v.Mesero.NombreCompleto.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(v.Ubicacion?.NombreUbicacion) && v.Ubicacion.NombreUbicacion.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void VerFactura()
        {
            if (Seleccionada is null)
            {
                Dialogo.Informar("Factura", "No hay una venta seleccionada.");
                return;
            }

            var facturas = Repositorio.ObtenerFacturasPorVenta(Seleccionada.IdVenta);
            if (facturas is null || facturas.Count == 0)
            {
                Dialogo.Informar("Factura", "No hay factura para esta venta.");
                return;
            }

            // Mostrar la primera factura encontrada (en este sistema cada venta genera 1 factura)
            var f = facturas[0];
            var vm = new ViewModels.FacturaViewModel(f);
            Dialogo.MostrarFactura(vm);
        }
    }
}
