using System.Collections.ObjectModel;
using System.Linq;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Pantalla de entrada del administrador: el estado del negocio de un vistazo,
    /// más el detalle de las alertas de inventario que exige RF-08.
    /// </summary>
    public class AdminResumenViewModel : SeccionAdminViewModel
    {
        public AdminResumenViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, "Resumen", "ViewDashboardOutline",
                   "Estado del bar en el día de hoy")
        {
            Alertas = new ObservableCollection<AlertaStock>();
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

        private int _cantidadUsuarios;
        public int CantidadUsuarios
        {
            get => _cantidadUsuarios;
            private set => SetProperty(ref _cantidadUsuarios, value);
        }

        private int _productosConReceta;
        public int ProductosConReceta
        {
            get => _productosConReceta;
            private set => SetProperty(ref _productosConReceta, value);
        }

        public bool HayAlertas => Alertas.Count > 0;

        public string TextoAlertas => Alertas.Count == 0
            ? "Todo el inventario está por encima del mínimo."
            : Alertas.Count == 1
                ? "1 ítem llegó al stock mínimo"
                : $"{Alertas.Count} ítems llegaron al stock mínimo";

        public sealed override void Recargar()
        {
            var ventas = Repositorio.ObtenerVentas();
            var hoy = ventas.Where(v => v.EstadoVenta == EstadoVenta.Confirmada &&
                                        v.FechaHora.Date == System.DateTime.Today).ToList();

            VentasDelDia = hoy.Count;
            FacturadoHoy = hoy.Sum(v => v.Total);

            var productos = Repositorio.ObtenerProductos();
            CantidadProductos = productos.Count;
            ProductosConReceta = productos.Count(p => p.TieneReceta);
            CantidadInsumos = Repositorio.ObtenerIngredientes().Count;
            CantidadUsuarios = Repositorio.ObtenerUsuarios().Count;

            Alertas.Clear();
            foreach (var alerta in Repositorio.ObtenerAlertasStock())
                Alertas.Add(alerta);

            OnPropertyChanged(nameof(HayAlertas));
            OnPropertyChanged(nameof(TextoAlertas));
        }
    }
}
