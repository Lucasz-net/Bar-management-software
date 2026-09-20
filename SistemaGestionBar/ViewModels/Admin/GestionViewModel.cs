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
    /// Cómo viene el bar. Es la pantalla de entrada del <b>gerente</b> y la diferencia de
    /// fondo entre su tablero y el del administrador.
    ///
    /// <b>Por qué es otra sección y no el mismo Resumen.</b> El resumen del administrador
    /// contesta "¿qué hay cargado en el sistema?": cuántos productos, cuántos insumos,
    /// cuántas cuentas. Son números de configuración, y no cambian de un día para otro.
    /// El gerente necesita la otra pregunta, la que cambia todos los días: cuánto se
    /// facturó, si fue más o menos que ayer, qué se vendió, quién vendió y qué falta
    /// reponer. Meter las dos en una pantalla daba una grilla de números sin un tema.
    ///
    /// El administrador tiene las dos secciones: no pierde nada y gana esta.
    ///
    /// <b>De dónde salen los números.</b> De <c>ObtenerVentas()</c>, que ya trae el método
    /// de pago, el cajero y el producto de cada renglón. Se agrupa en memoria, igual que
    /// hacen los reportes: sobre las ventas de un bar es intrascendente y evita cinco
    /// consultas más.
    /// </summary>
    public class GestionViewModel : SeccionAdminViewModel
    {
        /// <summary>
        /// Ventana de los rankings. El día suelto es muy poca muestra —un bar puede abrir
        /// y vender tres cosas— así que "lo que más se vende" se mira sobre la semana.
        /// </summary>
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

        // ---------------------------------------------------------------
        // El día
        // ---------------------------------------------------------------
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

        // ---------------------------------------------------------------
        // La comparación con ayer
        // ---------------------------------------------------------------
        private decimal _facturadoAyer;
        public decimal FacturadoAyer
        {
            get => _facturadoAyer;
            private set => SetProperty(ref _facturadoAyer, value);
        }

        private bool _huboVentasAyer;

        /// <summary>
        /// Sin ventas ayer no hay porcentaje que calcular: dividir por cero daría un
        /// "+∞%" que no dice nada. La vista muestra el texto plano en ese caso.
        /// </summary>
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

        /// <summary>"12% más que ayer", "8% menos que ayer" o por qué no se puede comparar.</summary>
        public string TextoComparacion
        {
            get => _textoComparacion;
            private set => SetProperty(ref _textoComparacion, value);
        }

        // ---------------------------------------------------------------
        // Los rankings de la semana
        // ---------------------------------------------------------------
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

        // ---------------------------------------------------------------
        // Reposición
        // ---------------------------------------------------------------
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

        /// <summary>
        /// Igual que en el resumen del administrador: reponer no cambia el stock acá, lleva
        /// a la pantalla donde ese ítem se edita. Poner un número a ojo desde una alerta
        /// sería saltearse la validación del ABM.
        /// </summary>
        private void Reponer(AlertaStock alerta)
        {
            if (alerta is null)
                return;

            IrA(alerta.EsInsumo ? DestinoAdmin.Inventario : DestinoAdmin.Productos, alerta);
        }

        // ---------------------------------------------------------------
        // Cálculo
        // ---------------------------------------------------------------
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
            // Sin ventas hoy el porcentaje daría "100% menos que ayer", que es cierto y no
            // sirve para nada: a las once de la mañana un bar todavía no vendió y eso no es
            // una caída del 100%. Se dice el hecho en vez del número.
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

        /// <summary>
        /// Qué se vendió, quién vendió y cómo se pagó, sobre la ventana del ranking.
        ///
        /// Los nombres se resuelven contra el padrón y el catálogo, no contra la
        /// navegación de cada venta: las consultas usan AsNoTracking, así que cada venta
        /// trae su propia copia del usuario y del producto, y el rol ni siquiera viene
        /// incluido. Agrupar por id y buscar el nombre aparte es lo que hacen los reportes.
        /// </summary>
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
