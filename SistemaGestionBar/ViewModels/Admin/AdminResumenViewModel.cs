using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
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
            IrAEmpleadosCommand = new RelayCommand(() => IrA(DestinoAdmin.Usuarios),
                                                   () => PuedeIrA(DestinoAdmin.Usuarios));
            ReponerCommand = new RelayCommand<AlertaStock>(Reponer);

            Recargar();
        }

        public ObservableCollection<AlertaStock> Alertas { get; }

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

        private int _cantidadEmpleados;
        public int CantidadEmpleados
        {
            get => _cantidadEmpleados;
            private set => SetProperty(ref _cantidadEmpleados, value);
        }

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

        public int ProductosDeVentaDirecta => Math.Max(0, CantidadProductos - ProductosConReceta);

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

        public bool HayAlertas => Alertas.Count > 0;

        public string TextoAlertas => Alertas.Count == 0
            ? "Todo el inventario está por encima del mínimo."
            : Alertas.Count == 1
                ? "1 ítem llegó al stock mínimo"
                : $"{Alertas.Count} ítems llegaron al stock mínimo";

        public ICommand IrAVentasCommand { get; }
        public ICommand IrAProductosCommand { get; }
        public ICommand IrAInventarioCommand { get; }
        public ICommand IrAEmpleadosCommand { get; }
        public ICommand ReponerCommand { get; }

        public bool PuedeVerEmpleados => PuedeIrA(DestinoAdmin.Usuarios);

        protected override void AlCambiarLosDestinos() =>
            OnPropertyChanged(nameof(PuedeVerEmpleados));

        private void Reponer(AlertaStock alerta) => IrAReponer(alerta);

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
