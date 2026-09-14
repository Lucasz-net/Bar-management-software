using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Pantalla de entrada del administrador: el estado del negocio de un vistazo,
    /// más el detalle de las alertas de inventario que exige RF-08.
    ///
    /// Todo lo que se muestra acá es un resumen de otra pantalla, así que todo lleva a
    /// esa pantalla: las tarjetas de arriba entran a la sección que resumen y cada alerta
    /// abre directamente el editor del insumo que hay que reponer. Un número que no se
    /// puede seguir obliga a buscar a mano en el menú lo que uno ya tenía delante.
    /// </summary>
    public class AdminResumenViewModel : SeccionAdminViewModel
    {
        public AdminResumenViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Resumen, "Resumen", "ViewDashboardOutline",
                   "Estado del bar en el día de hoy")
        {
            Alertas = new ObservableCollection<AlertaStock>();

            IrAVentasCommand = new RelayCommand(() => IrA(DestinoAdmin.Ventas));
            IrAProductosCommand = new RelayCommand(() => IrA(DestinoAdmin.Productos));
            IrAInventarioCommand = new RelayCommand(() => IrA(DestinoAdmin.Inventario));
            IrAEmpleadosCommand = new RelayCommand(() => IrA(DestinoAdmin.Personas),
                                                   () => PuedeIrA(DestinoAdmin.Personas));
            ReponerCommand = new RelayCommand<AlertaStock>(Reponer);

            Recargar();
        }

        public ObservableCollection<AlertaStock> Alertas { get; }

        // ---------------------------------------------------------------
        // Indicadores
        // ---------------------------------------------------------------
        private int _ventasDelDia;
        public int VentasDelDia
        {
            get => _ventasDelDia;
            private set => SetProperty(ref _ventasDelDia, value);
        }

        private decimal _facturadoHoy;
        public decimal FacturadoHoy
        {
            get => _facturadoHoy;
            private set => SetProperty(ref _facturadoHoy, value);
        }

        private int _cantidadProductos;
        public int CantidadProductos
        {
            get => _cantidadProductos;
            private set => SetProperty(ref _cantidadProductos, value);
        }

        private int _cantidadInsumos;
        public int CantidadInsumos
        {
            get => _cantidadInsumos;
            private set => SetProperty(ref _cantidadInsumos, value);
        }

        /// <summary>
        /// Empleados = personas con cuenta de acceso. Es el mismo número que antes se
        /// rotulaba "cuentas de acceso", pero el rótulo nombraba el mecanismo en vez de
        /// la gente: en la sección Personas ese mismo grupo ya se llama "empleados".
        /// </summary>
        private int _cantidadEmpleados;
        public int CantidadEmpleados
        {
            get => _cantidadEmpleados;
            private set => SetProperty(ref _cantidadEmpleados, value);
        }

        // ---------------------------------------------------------------
        // Composición del catálogo
        // ---------------------------------------------------------------
        private int _productosConReceta;
        public int ProductosConReceta
        {
            get => _productosConReceta;
            private set
            {
                if (SetProperty(ref _productosConReceta, value))
                    RefrescarComposicion();
            }
        }

        /// <summary>Los que no llevan receta: botellas y latas, que descuentan su propio stock.</summary>
        public int ProductosDeVentaDirecta => Math.Max(0, CantidadProductos - ProductosConReceta);

        /// <summary>Qué porción del catálogo se prepara. Alimenta la barra de proporción.</summary>
        public double ProporcionConReceta =>
            CantidadProductos == 0 ? 0 : (double)ProductosConReceta / CantidadProductos;

        public string PorcentajeConReceta => $"{ProporcionConReceta:P0}";
        public string PorcentajeVentaDirecta => $"{1 - ProporcionConReceta:P0}";

        private void RefrescarComposicion()
        {
            OnPropertyChanged(nameof(ProductosDeVentaDirecta));
            OnPropertyChanged(nameof(ProporcionConReceta));
            OnPropertyChanged(nameof(PorcentajeConReceta));
            OnPropertyChanged(nameof(PorcentajeVentaDirecta));
        }

        // ---------------------------------------------------------------
        // Alertas
        // ---------------------------------------------------------------
        public bool HayAlertas => Alertas.Count > 0;

        public string TextoAlertas => Alertas.Count == 0
            ? "Todo el inventario está por encima del mínimo."
            : Alertas.Count == 1
                ? "1 ítem llegó al stock mínimo"
                : $"{Alertas.Count} ítems llegaron al stock mínimo";

        // ---------------------------------------------------------------
        // Atajos
        // ---------------------------------------------------------------
        public ICommand IrAVentasCommand { get; }
        public ICommand IrAProductosCommand { get; }
        public ICommand IrAInventarioCommand { get; }
        public ICommand IrAEmpleadosCommand { get; }
        public ICommand ReponerCommand { get; }

        /// <summary>Un gerente no tiene la sección Personas: la tarjeta no se muestra (RF-09).</summary>
        public bool PuedeVerEmpleados => PuedeIrA(DestinoAdmin.Personas);

        protected override void AlCambiarLosDestinos() =>
            OnPropertyChanged(nameof(PuedeVerEmpleados));

        /// <summary>
        /// Reponer no cambia el stock acá: lleva a la pantalla donde ese ítem se edita y
        /// deja el editor abierto en él. Poner un número a ojo desde una alerta sería
        /// saltearse la validación del ABM, que es la que sabe qué es un stock válido.
        /// </summary>
        private void Reponer(AlertaStock alerta)
        {
            if (alerta is null)
                return;

            IrA(alerta.EsInsumo ? DestinoAdmin.Inventario : DestinoAdmin.Productos, alerta);
        }

        public sealed override void Recargar()
        {
            var ventas = Repositorio.ObtenerVentas();
            var hoy = ventas.Where(v => v.EstadoVenta == EstadoVenta.Confirmada &&
                                        v.FechaHora.Date == DateTime.Today).ToList();

            VentasDelDia = hoy.Count;
            FacturadoHoy = hoy.Sum(v => v.Total);

            var productos = Repositorio.ObtenerProductos();
            CantidadProductos = productos.Count;
            ProductosConReceta = productos.Count(p => p.TieneReceta);
            CantidadInsumos = Repositorio.ObtenerIngredientes().Count;
            CantidadEmpleados = Repositorio.ObtenerUsuarios().Count;

            Alertas.Clear();
            foreach (var alerta in Repositorio.ObtenerAlertasStock())
                Alertas.Add(alerta);

            RefrescarComposicion();
            OnPropertyChanged(nameof(HayAlertas));
            OnPropertyChanged(nameof(TextoAlertas));
        }
    }
}
