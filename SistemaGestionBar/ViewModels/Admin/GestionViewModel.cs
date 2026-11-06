using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class GestionViewModel : SeccionAdminViewModel
    {
        private const int DiasDelRanking = 7;

        public GestionViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Gestion, "Gestión", "ChartTimelineVariant",
                   "Cómo viene el bar hoy: facturación, qué se vende y qué falta reponer")
        {
            Alertas = new ObservableCollection<AlertaStock>();

            IrAVentasCommand = new RelayCommand(() => IrA(DestinoAdmin.Ventas));
            IrAEquipoCommand = new RelayCommand(() => IrA(DestinoAdmin.Equipo),
                                                () => PuedeIrA(DestinoAdmin.Equipo));
            IrAReportesCommand = new RelayCommand(() => IrA(DestinoAdmin.Reportes),
                                                  () => PuedeIrA(DestinoAdmin.Reportes));
            IrAInventarioCommand = new RelayCommand(() => IrA(DestinoAdmin.Inventario),
                                                    () => PuedeIrA(DestinoAdmin.Inventario));
            ReponerCommand = new RelayCommand<AlertaStock>(Reponer);

            Recargar();
        }

        private int _ventasHoy;
        public int VentasHoy
        {
            get => _ventasHoy;
            private set => SetProperty(ref _ventasHoy, value);
        }

        private decimal _facturadoHoy;
        public decimal FacturadoHoy
        {
            get => _facturadoHoy;
            private set => SetProperty(ref _facturadoHoy, value);
        }

        private decimal _ticketPromedio;
        public decimal TicketPromedio
        {
            get => _ticketPromedio;
            private set => SetProperty(ref _ticketPromedio, value);
        }

        private decimal _facturadoAyer;
        public decimal FacturadoAyer
        {
            get => _facturadoAyer;
            private set => SetProperty(ref _facturadoAyer, value);
        }

        private bool _huboVentasAyer;

        public bool HayComparacion
        {
            get => _huboVentasAyer;
            private set
            {
                if (SetProperty(ref _huboVentasAyer, value))
                    OnPropertyChanged(nameof(NoHayComparacion));
            }
        }

        public bool NoHayComparacion => !HayComparacion;

        private bool _mejorQueAyer;
        public bool MejorQueAyer
        {
            get => _mejorQueAyer;
            private set => SetProperty(ref _mejorQueAyer, value);
        }

        private string _textoComparacion = string.Empty;

        public string TextoComparacion
        {
            get => _textoComparacion;
            private set => SetProperty(ref _textoComparacion, value);
        }

        public string TituloDelRanking => $"Últimos {DiasDelRanking} días";

        private string _productoMasVendido = SinDatos;
        public string ProductoMasVendido
        {
            get => _productoMasVendido;
            private set => SetProperty(ref _productoMasVendido, value);
        }

        private string _detalleProductoMasVendido = string.Empty;
        public string DetalleProductoMasVendido
        {
            get => _detalleProductoMasVendido;
            private set => SetProperty(ref _detalleProductoMasVendido, value);
        }

        private string _quienMasVendio = SinDatos;
        public string QuienMasVendio
        {
            get => _quienMasVendio;
            private set => SetProperty(ref _quienMasVendio, value);
        }

        private string _detalleQuienMasVendio = string.Empty;
        public string DetalleQuienMasVendio
        {
            get => _detalleQuienMasVendio;
            private set => SetProperty(ref _detalleQuienMasVendio, value);
        }

        private string _metodoMasUsado = SinDatos;
        public string MetodoMasUsado
        {
            get => _metodoMasUsado;
            private set => SetProperty(ref _metodoMasUsado, value);
        }

        private string _detalleMetodoMasUsado = string.Empty;
        public string DetalleMetodoMasUsado
        {
            get => _detalleMetodoMasUsado;
            private set => SetProperty(ref _detalleMetodoMasUsado, value);
        }

        private const string SinDatos = "—";

        public ObservableCollection<AlertaStock> Alertas { get; }

        public bool HayAlertas => Alertas.Count > 0;

        public string TextoAlertas => Alertas.Count switch
        {
            0 => "Todo el inventario está por encima del mínimo.",
            1 => "1 ítem llegó al stock mínimo",
            _ => $"{Alertas.Count} ítems llegaron al stock mínimo"
        };

        public ICommand IrAVentasCommand { get; }
        public ICommand IrAEquipoCommand { get; }
        public ICommand IrAReportesCommand { get; }
        public ICommand IrAInventarioCommand { get; }
        public ICommand ReponerCommand { get; }

        public bool PuedeVerEquipo => PuedeIrA(DestinoAdmin.Equipo);

        protected override void AlCambiarLosDestinos() =>
            OnPropertyChanged(nameof(PuedeVerEquipo));

        private void Reponer(AlertaStock alerta) => IrAReponer(alerta);

        public sealed override void Recargar()
        {
            var confirmadas = Repositorio.ObtenerVentas()
                                         .Where(v => v.EstadoVenta == EstadoVenta.Confirmada)
                                         .ToList();

            var hoy = DateTime.Today;
            var ayer = hoy.AddDays(-1);

            var deHoy = confirmadas.Where(v => v.FechaHora.Date == hoy).ToList();
            var deAyer = confirmadas.Where(v => v.FechaHora.Date == ayer).ToList();

            VentasHoy = deHoy.Count;
            FacturadoHoy = deHoy.Sum(v => v.Total);
            TicketPromedio = deHoy.Count == 0 ? 0 : FacturadoHoy / deHoy.Count;

            FacturadoAyer = deAyer.Sum(v => v.Total);
            CalcularComparacion();

            CalcularRankings(confirmadas.Where(v => v.FechaHora.Date > hoy.AddDays(-DiasDelRanking)).ToList());

            Alertas.Clear();
            foreach (var alerta in Repositorio.ObtenerAlertasStock())
                Alertas.Add(alerta);

            OnPropertyChanged(nameof(HayAlertas));
            OnPropertyChanged(nameof(TextoAlertas));
        }

        private void CalcularComparacion()
        {
            if (FacturadoHoy <= 0)
            {
                HayComparacion = false;
                MejorQueAyer = false;
                TextoComparacion = "Todavía no hubo ventas hoy";
                return;
            }

            HayComparacion = FacturadoAyer > 0;

            if (!HayComparacion)
            {
                MejorQueAyer = true;
                TextoComparacion = "Ayer no hubo ventas para comparar";
                return;
            }

            decimal variacion = (FacturadoHoy - FacturadoAyer) / FacturadoAyer * 100m;
            MejorQueAyer = variacion >= 0;

            TextoComparacion = variacion switch
            {
                0 => "Igual que ayer",
                > 0 => $"{variacion:0.#}% más que ayer",
                _ => $"{Math.Abs(variacion):0.#}% menos que ayer"
            };
        }

        private void CalcularRankings(List<Venta> ventas)
        {
            if (ventas.Count == 0)
            {
                LimpiarRankings();
                return;
            }

            var productos = Repositorio.ObtenerProductos().ToDictionary(p => p.IdProducto);
            var empleados = Repositorio.ObtenerUsuarios().ToDictionary(u => u.IdUsuario);

            var renglones = ventas.SelectMany(v => v.Detalles).ToList();

            var topProducto = renglones.GroupBy(d => d.IdProducto)
                                       .Select(g => new { Id = g.Key, Unidades = g.Sum(d => d.Cantidad) })
                                       .OrderByDescending(g => g.Unidades)
                                       .FirstOrDefault();

            if (topProducto is null)
            {
                ProductoMasVendido = SinDatos;
                DetalleProductoMasVendido = "No se vendió nada en el período";
            }
            else
            {
                ProductoMasVendido = productos.TryGetValue(topProducto.Id, out var producto)
                    ? producto.Nombre
                    : SinDatos;
                DetalleProductoMasVendido = $"{topProducto.Unidades} unidades vendidas";
            }

            var topEmpleado = ventas.GroupBy(v => v.IdCajero)
                                    .Select(g => new { Id = g.Key, Importe = g.Sum(v => v.Total) })
                                    .OrderByDescending(g => g.Importe)
                                    .First();

            QuienMasVendio = empleados.TryGetValue(topEmpleado.Id, out var cajero)
                ? cajero.NombreCompleto
                : SinDatos;
            DetalleQuienMasVendio = $"{topEmpleado.Importe:C0} cobrados";

            decimal total = ventas.Sum(v => v.Total);

            var topMetodo = ventas.GroupBy(v => v.MetodoPago?.NombreMetodo ?? SinDatos)
                                  .Select(g => new { Nombre = g.Key, Importe = g.Sum(v => v.Total) })
                                  .OrderByDescending(g => g.Importe)
                                  .First();

            MetodoMasUsado = topMetodo.Nombre;
            DetalleMetodoMasUsado = total <= 0
                ? string.Empty
                : $"{topMetodo.Importe / total * 100m:0}% de lo facturado";

            OnPropertyChanged(nameof(TituloDelRanking));
        }

        private void LimpiarRankings()
        {
            ProductoMasVendido = SinDatos;
            QuienMasVendio = SinDatos;
            MetodoMasUsado = SinDatos;

            const string vacio = "Sin ventas en el período";
            DetalleProductoMasVendido = vacio;
            DetalleQuienMasVendio = vacio;
            DetalleMetodoMasUsado = vacio;
        }
    }
}
