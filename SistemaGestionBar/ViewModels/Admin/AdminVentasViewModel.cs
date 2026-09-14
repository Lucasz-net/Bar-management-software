using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Historial de ventas con su detalle y su factura (RF-01).
    ///
    /// Es de solo lectura a propósito: una venta cobrada no se edita. Si estuvo mal,
    /// se anula y se rehace; editarla dejaría el stock descontado sin correspondencia.
    /// </summary>
    public class AdminVentasViewModel : SeccionAdminViewModel
    {
        public AdminVentasViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Ventas, "Ventas", "ReceiptTextOutline",
                   "Historial de ventas con su detalle, cajero, mesero, ubicación y factura")
        {
            Ventas = new ObservableCollection<Venta>();
            Detalle = new ObservableCollection<VentaDetalle>();

            VerFacturaCommand = new RelayCommand(VerFactura, () => Seleccionada is not null);

            Recargar();
        }

        public ObservableCollection<Venta> Ventas { get; }
        public ObservableCollection<VentaDetalle> Detalle { get; }

        // ---------------------------------------------------------------
        // Filtros: texto, período y modalidad
        // ---------------------------------------------------------------
        public IReadOnlyList<OpcionFiltro<PeriodoVenta>> OpcionesPeriodo { get; } = new[]
        {
            new OpcionFiltro<PeriodoVenta>("Todo el historial", PeriodoVenta.Todo),
            new OpcionFiltro<PeriodoVenta>("Hoy", PeriodoVenta.Hoy),
            new OpcionFiltro<PeriodoVenta>("Última semana", PeriodoVenta.UltimaSemana),
            new OpcionFiltro<PeriodoVenta>("Último mes", PeriodoVenta.UltimoMes)
        };

        private OpcionFiltro<PeriodoVenta>? _periodo;
        public OpcionFiltro<PeriodoVenta>? Periodo
        {
            get => _periodo;
            set
            {
                if (SetProperty(ref _periodo, value))
                    RefrescarVista();
            }
        }

        public IReadOnlyList<OpcionFiltro<ModalidadConsumo?>> OpcionesModalidad { get; } = new[]
        {
            new OpcionFiltro<ModalidadConsumo?>("Todas las modalidades", null),
            new OpcionFiltro<ModalidadConsumo?>("En el local", ModalidadConsumo.Local),
            new OpcionFiltro<ModalidadConsumo?>("Para llevar", ModalidadConsumo.ParaLlevar)
        };

        private OpcionFiltro<ModalidadConsumo?>? _modalidad;
        public OpcionFiltro<ModalidadConsumo?>? Modalidad
        {
            get => _modalidad;
            set
            {
                if (SetProperty(ref _modalidad, value))
                    RefrescarVista();
            }
        }

        public override bool HayFiltroAplicado =>
            HayBusqueda
            || (Periodo is not null && Periodo.Valor != PeriodoVenta.Todo)
            || (Modalidad is not null && Modalidad.Valor is not null);

        protected override bool Coincide(object item)
        {
            if (item is not Venta venta)
                return false;

            if (Modalidad?.Valor is ModalidadConsumo modalidad && venta.ModalidadConsumo != modalidad)
                return false;

            var desde = FechaDesde();
            if (desde is not null && venta.FechaHora < desde)
                return false;

            if (!HayBusqueda)
                return true;

            return venta.IdVenta.ToString().Contains(Termino)
                || Contiene(venta.Cliente?.NombreMostrado, Termino)
                || Contiene(venta.Cajero?.NombreCompleto, Termino)
                || Contiene(venta.Mesero?.NombreCompleto, Termino)
                || Contiene(venta.Ubicacion?.NombreUbicacion, Termino)
                || Contiene(venta.MetodoPago?.NombreMetodo, Termino);
        }

        /// <summary>Fecha de corte del período elegido, o null si no hay corte.</summary>
        private DateTime? FechaDesde() => (Periodo?.Valor ?? PeriodoVenta.Todo) switch
        {
            PeriodoVenta.Hoy => DateTime.Today,
            PeriodoVenta.UltimaSemana => DateTime.Today.AddDays(-7),
            PeriodoVenta.UltimoMes => DateTime.Today.AddMonths(-1),
            _ => null
        };

        protected override void LimpiarFiltros()
        {
            base.LimpiarFiltros();
            Periodo = OpcionesPeriodo[0];
            Modalidad = OpcionesModalidad[0];
        }

        // ---------------------------------------------------------------
        // Selección y detalle
        // ---------------------------------------------------------------
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
                    foreach (var linea in value.Detalles)
                        Detalle.Add(linea);
                }

                OnPropertyChanged(nameof(HaySeleccion));
                OnPropertyChanged(nameof(TituloDetalle));
                OnPropertyChanged(nameof(FacturaDeLaVenta));
                OnPropertyChanged(nameof(TieneFactura));
            }
        }

        public bool HaySeleccion => Seleccionada is not null;

        public string TituloDetalle => Seleccionada is null
            ? "Detalle de la venta"
            : $"Venta #{Seleccionada.IdVenta}";

        /// <summary>Comprobante de la venta seleccionada, para mostrar su número en el panel.</summary>
        public Factura? FacturaDeLaVenta =>
            Seleccionada is null ? null : Repositorio.ObtenerFacturaDeVenta(Seleccionada.IdVenta);

        public bool TieneFactura => FacturaDeLaVenta is not null;

        // ---------------------------------------------------------------
        // Indicadores. Se calculan sobre lo que se ve: si el usuario filtra
        // "hoy", los números tienen que ser los de hoy y no los del historial.
        // ---------------------------------------------------------------
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

        public ICommand VerFacturaCommand { get; }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionada?.IdVenta;

            Ventas.Clear();
            foreach (var venta in Repositorio.ObtenerVentas().OrderByDescending(v => v.IdVenta))
                Ventas.Add(venta);

            ConfigurarFiltro(Ventas);
            _periodo ??= OpcionesPeriodo[0];
            _modalidad ??= OpcionesModalidad[0];
            RefrescarVista();

            Seleccionada = Ventas.FirstOrDefault(v => v.IdVenta == idPrevio) ?? Ventas.FirstOrDefault();
        }

        /// <summary>
        /// Los indicadores se recalculan cada vez que cambia el filtro, no solo al recargar.
        /// </summary>
        private void RecalcularIndicadores()
        {
            var visibles = Ventas.Where(v => Coincide(v)).ToList();

            CantidadVentas = visibles.Count;
            TotalFacturado = visibles.Sum(v => v.Total);
            TicketPromedio = visibles.Count == 0 ? 0 : TotalFacturado / visibles.Count;
        }

        protected override void RefrescarIndicadores() => RecalcularIndicadores();

        private void VerFactura()
        {
            if (Seleccionada is null)
            {
                Fallar("No hay una venta seleccionada.");
                return;
            }

            var factura = Repositorio.ObtenerFacturaDeVenta(Seleccionada.IdVenta);
            if (factura is null)
            {
                Fallar($"La venta #{Seleccionada.IdVenta} no tiene factura emitida.");
                return;
            }

            Dialogo.MostrarFactura(new FacturaViewModel(factura));
        }
    }
}
