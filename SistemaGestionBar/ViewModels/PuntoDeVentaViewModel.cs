using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// Pantalla principal de ventas (RF-01 a RF-07).
    /// Reemplaza al Dashboard con code-behind: acá vive todo el estado y la lógica,
    /// y el XAML solo se bindea. Analogía web: este es el componente contenedor con
    /// el estado; PuntoDeVentaView.xaml es su plantilla.
    /// </summary>
    public class PuntoDeVentaViewModel : ViewModelBase
    {
        private readonly IRepositorioBar _repositorio;
        private readonly IServicioDialogo _dialogo;

        public PuntoDeVentaViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo, Usuario usuarioActual)
        {
            _repositorio = repositorio;
            _dialogo = dialogo;
            UsuarioActual = usuarioActual;

            Productos = new ObservableCollection<ProductoCatalogoViewModel>(
                _repositorio.ObtenerProductos()
                            .Select(p => new ProductoCatalogoViewModel(p, _repositorio.CalcularDisponibilidad(p))));

            // La "vista" de la colección es la que se filtra. La colección original queda intacta.
            VistaProductos = CollectionViewSource.GetDefaultView(Productos);
            VistaProductos.Filter = FiltrarProducto;

            Categorias = ConstruirCategorias();
            Ticket = new ObservableCollection<LineaTicketViewModel>();
            Ticket.CollectionChanged += (_, _) => RefrescarTotales();

            Clientes = new ObservableCollection<Cliente>(_repositorio.ObtenerClientes());
            MetodosPago = new ObservableCollection<MetodoPago>(_repositorio.ObtenerMetodosPago());
            Ubicaciones = new ObservableCollection<Ubicacion>(_repositorio.ObtenerUbicaciones());
            Meseros = new ObservableCollection<Usuario>(_repositorio.ObtenerMeseros());
            AlertasStock = new ObservableCollection<AlertaStock>();

            // Valores por defecto: Consumidor Final (RF-05) y efectivo (RF-06).
            _clienteSeleccionado = Clientes.FirstOrDefault();
            _metodoPagoSeleccionado = MetodosPago.FirstOrDefault();
            _meseroSeleccionado = Meseros.FirstOrDefault(m => m.IdUsuario == UsuarioActual.IdUsuario) ?? Meseros.FirstOrDefault();

            AgregarProductoCommand = new RelayCommand<ProductoCatalogoViewModel>(AgregarProducto, p => !p.SinStock);
            QuitarUnidadCommand = new RelayCommand<LineaTicketViewModel>(QuitarUnidad);
            EliminarLineaCommand = new RelayCommand<LineaTicketViewModel>(EliminarLinea);
            VerRecetaCommand = new RelayCommand<ProductoCatalogoViewModel>(VerReceta, p => p.TieneReceta);
            SeleccionarCategoriaCommand = new RelayCommand<CategoriaFiltroViewModel>(SeleccionarCategoria);
            ConfirmarVentaCommand = new RelayCommand(ConfirmarVenta, () => Ticket.Count > 0);
            CancelarTicketCommand = new RelayCommand(CancelarTicket, () => Ticket.Count > 0);
            CerrarSesionCommand = new RelayCommand(() => CierreSesionSolicitado?.Invoke(this, EventArgs.Empty));
            VolverAlTableroCommand = new RelayCommand(() => TableroSolicitado?.Invoke(this, EventArgs.Empty));

            RefrescarIndicadores();
        }

        /// <summary>Lo escucha MainViewModel para volver al login (RF-09).</summary>
        public event EventHandler? CierreSesionSolicitado;

        /// <summary>
        /// Vuelta al tablero de administración. Es una acción distinta de cerrar sesión:
        /// antes el mismo botón hacía las dos cosas según el rol, y desde el punto de venta
        /// no había forma de saber que uno volvía al tablero en lugar de salir del sistema.
        /// </summary>
        public event EventHandler? TableroSolicitado;

        // ---------------------------------------------------------------
        // Encabezado
        // ---------------------------------------------------------------
        public Usuario UsuarioActual { get; }

        public string NombreUsuario => UsuarioActual.NombreCompleto;
        public string RolUsuario => UsuarioActual.NombreRol;
        public string InicialesUsuario =>
            string.Concat(NombreUsuario.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                       .Take(2)
                                       .Select(p => char.ToUpperInvariant(p[0])));

        /// <summary>Ej: "Domingo, 06 de septiembre". Solo se capitaliza la primera letra.</summary>
        public string FechaActual
        {
            get
            {
                string fecha = DateTime.Now.ToString("dddd, dd 'de' MMMM", CultureInfo.CurrentCulture);
                return string.IsNullOrEmpty(fecha) ? fecha : char.ToUpper(fecha[0]) + fecha[1..];
            }
        }

        private int _ventasDelDia;
        public int VentasDelDia
        {
            get => _ventasDelDia;
            private set
            {
                if (SetProperty(ref _ventasDelDia, value))
                    OnPropertyChanged(nameof(TextoVentasDelDia));
            }
        }

        public string TextoVentasDelDia => VentasDelDia == 1 ? "1 venta hoy" : $"{VentasDelDia} ventas hoy";

        // ---------------------------------------------------------------
        // Catálogo
        // ---------------------------------------------------------------
        public ObservableCollection<ProductoCatalogoViewModel> Productos { get; }

        public ICollectionView VistaProductos { get; }

        public ObservableCollection<CategoriaFiltroViewModel> Categorias { get; }

        private string _textoBusqueda = string.Empty;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                if (SetProperty(ref _textoBusqueda, value))
                    VistaProductos.Refresh();
            }
        }

        private CategoriaFiltroViewModel? _categoriaSeleccionada;

        private bool FiltrarProducto(object item)
        {
            if (item is not ProductoCatalogoViewModel producto)
                return false;

            bool coincideCategoria = _categoriaSeleccionada?.IdCategoria is not int idCategoria
                                     || producto.IdCategoria == idCategoria;

            bool coincideTexto = string.IsNullOrWhiteSpace(TextoBusqueda)
                                 || producto.CoincideConBusqueda(TextoBusqueda.Trim());

            return coincideCategoria && coincideTexto;
        }

        private ObservableCollection<CategoriaFiltroViewModel> ConstruirCategorias()
        {
            var lista = new ObservableCollection<CategoriaFiltroViewModel>
            {
                new CategoriaFiltroViewModel(null, "Todos", Productos.Count) { EstaSeleccionada = true }
            };

            foreach (var categoria in _repositorio.ObtenerCategorias())
            {
                int cantidad = Productos.Count(p => p.IdCategoria == categoria.IdCategoria);
                lista.Add(new CategoriaFiltroViewModel(categoria, categoria.NombreCategoria, cantidad));
            }

            return lista;
        }

        private void SeleccionarCategoria(CategoriaFiltroViewModel categoria)
        {
            _categoriaSeleccionada = categoria.IdCategoria is null ? null : categoria;

            foreach (var item in Categorias)
                item.EstaSeleccionada = ReferenceEquals(item, categoria);

            VistaProductos.Refresh();
        }

        // ---------------------------------------------------------------
        // Ticket (Venta + Venta_Detalle)
        // ---------------------------------------------------------------
        public ObservableCollection<LineaTicketViewModel> Ticket { get; }

        /// <summary>RF-01: total dinámico, sumatoria de los subtotales del ticket.</summary>
        public decimal Total => Ticket.Sum(l => l.Subtotal);

        public int CantidadUnidades => Ticket.Sum(l => l.Cantidad);

        public bool TicketVacio => Ticket.Count == 0;

        private void AgregarProducto(ProductoCatalogoViewModel producto)
        {
            var linea = Ticket.FirstOrDefault(l => l.IdProducto == producto.IdProducto);
            int enTicket = linea?.Cantidad ?? 0;

            if (enTicket >= producto.Disponibilidad)
            {
                MostrarMensaje($"Solo quedan {producto.Disponibilidad} unidades de {producto.Nombre}.", esError: true);
                return;
            }

            if (linea is null)
            {
                linea = new LineaTicketViewModel(producto);
                linea.PropertyChanged += LineaCambiada;
                Ticket.Add(linea);
            }
            else
            {
                linea.Cantidad++;
            }

            producto.CantidadEnTicket = linea.Cantidad;
            LimpiarMensaje();
            RefrescarTotales();
        }

        private void QuitarUnidad(LineaTicketViewModel linea)
        {
            if (linea.Cantidad > 1)
            {
                linea.Cantidad--;
                linea.Producto.CantidadEnTicket = linea.Cantidad;
                RefrescarTotales();
            }
            else
            {
                EliminarLinea(linea);
            }
        }

        private void EliminarLinea(LineaTicketViewModel linea)
        {
            linea.PropertyChanged -= LineaCambiada;
            linea.Producto.CantidadEnTicket = 0;
            Ticket.Remove(linea);
            RefrescarTotales();
        }

        private void LineaCambiada(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LineaTicketViewModel.Subtotal))
                RefrescarTotales();
        }

        private void CancelarTicket()
        {
            if (!_dialogo.Confirmar("Cancelar pedido", "Se van a quitar todos los productos del ticket. ¿Continuar?"))
                return;

            VaciarTicket();
            MostrarMensaje("Pedido cancelado.", esError: false);
        }

        private void VaciarTicket()
        {
            foreach (var linea in Ticket.ToList())
            {
                linea.PropertyChanged -= LineaCambiada;
                linea.Producto.CantidadEnTicket = 0;
            }

            Ticket.Clear();
            RefrescarTotales();
        }

        private void RefrescarTotales()
        {
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(CantidadUnidades));
            OnPropertyChanged(nameof(TicketVacio));

            // Los botones Confirmar/Cancelar dependen de si hay renglones: sin esto
            // CommandManager solo reevalua CanExecute ante interaccion del usuario y
            // los botones pueden quedar deshabilitados de mas.
            CommandManager.InvalidateRequerySuggested();
        }

        // ---------------------------------------------------------------
        // Modalidad, ubicación, cliente, pago (RF-02, RF-03, RF-05, RF-06)
        // ---------------------------------------------------------------
        public ObservableCollection<Cliente> Clientes { get; }
        public ObservableCollection<MetodoPago> MetodosPago { get; }
        public ObservableCollection<Ubicacion> Ubicaciones { get; }
        public ObservableCollection<Usuario> Meseros { get; }

        private bool _esConsumoLocal = true;
        /// <summary>RF-03. Si es false, la venta es "Para Llevar" y la ubicación queda nula.</summary>
        public bool EsConsumoLocal
        {
            get => _esConsumoLocal;
            set
            {
                if (!SetProperty(ref _esConsumoLocal, value))
                    return;

                OnPropertyChanged(nameof(EsParaLlevar));

                if (!value)
                {
                    // Para llevar no ocupa ubicación (RF-03) ni necesita mesero.
                    UbicacionSeleccionada = null;
                    MeseroSeleccionado = null;
                }

                OnPropertyChanged(nameof(RequiereMesero));
            }
        }

        public bool EsParaLlevar
        {
            get => !_esConsumoLocal;
            set => EsConsumoLocal = !value;
        }

        private Ubicacion? _ubicacionSeleccionada;
        public Ubicacion? UbicacionSeleccionada
        {
            get => _ubicacionSeleccionada;
            set
            {
                if (!SetProperty(ref _ubicacionSeleccionada, value))
                    return;

                OnPropertyChanged(nameof(RequiereMesero));

                // En la barra atiende el barman: la venta no lleva mesero (RF-02).
                if (!RequiereMesero)
                    MeseroSeleccionado = null;
                else
                    MeseroSeleccionado ??= Meseros.FirstOrDefault();
            }
        }

        /// <summary>
        /// Solo se pide mesero si la venta es en el local Y en una mesa.
        /// Gobierna la visibilidad del combo en la vista y la validación del repositorio.
        /// </summary>
        public bool RequiereMesero => EsConsumoLocal && (UbicacionSeleccionada?.RequiereMesero ?? false);

        private Usuario? _meseroSeleccionado;
        public Usuario? MeseroSeleccionado
        {
            get => _meseroSeleccionado;
            set => SetProperty(ref _meseroSeleccionado, value);
        }

        private Cliente? _clienteSeleccionado;
        public Cliente? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set => SetProperty(ref _clienteSeleccionado, value);
        }

        private MetodoPago? _metodoPagoSeleccionado;
        public MetodoPago? MetodoPagoSeleccionado
        {
            get => _metodoPagoSeleccionado;
            set => SetProperty(ref _metodoPagoSeleccionado, value);
        }

        // ---------------------------------------------------------------
        // Alertas de inventario (RF-08)
        // ---------------------------------------------------------------
        public ObservableCollection<AlertaStock> AlertasStock { get; }

        /// <summary>RF-08: el panel de alertas es exclusivo del administrador.</summary>
        public bool PuedeVerAlertas => UsuarioActual.AccedeAlTablero;

        /// <summary>
        /// Solo el administrador tiene tablero al que volver: para el resto
        /// el punto de venta es la única pantalla del sistema.
        /// </summary>
        public bool PuedeVolverAlTablero => UsuarioActual.AccedeAlTablero;

        public bool HayAlertasStock => PuedeVerAlertas && AlertasStock.Count > 0;

        public string TextoAlertas => AlertasStock.Count == 1
            ? "1 insumo bajo el mínimo"
            : $"{AlertasStock.Count} insumos bajo el mínimo";

        // ---------------------------------------------------------------
        // Mensajes de la barra inferior
        // ---------------------------------------------------------------
        private string _mensaje = string.Empty;
        public string Mensaje
        {
            get => _mensaje;
            private set
            {
                if (SetProperty(ref _mensaje, value))
                    OnPropertyChanged(nameof(HayMensaje));
            }
        }

        private bool _mensajeEsError;
        public bool MensajeEsError
        {
            get => _mensajeEsError;
            private set => SetProperty(ref _mensajeEsError, value);
        }

        public bool HayMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        private void MostrarMensaje(string texto, bool esError)
        {
            MensajeEsError = esError;
            Mensaje = texto;
        }

        private void LimpiarMensaje() => Mensaje = string.Empty;

        // ---------------------------------------------------------------
        // Comandos
        // ---------------------------------------------------------------
        public ICommand AgregarProductoCommand { get; }
        public ICommand QuitarUnidadCommand { get; }
        public ICommand EliminarLineaCommand { get; }
        public ICommand VerRecetaCommand { get; }
        public ICommand SeleccionarCategoriaCommand { get; }
        public ICommand ConfirmarVentaCommand { get; }
        public ICommand CancelarTicketCommand { get; }
        public ICommand CerrarSesionCommand { get; }
        public ICommand VolverAlTableroCommand { get; }

        private void VerReceta(ProductoCatalogoViewModel producto)
        {
            if (!producto.TieneReceta)
                return;

            _dialogo.MostrarReceta(new RecetaViewModel(producto.Producto));
        }

        /// <summary>
        /// RF-01 + RF-07: arma la Venta con su detalle y delega en el repositorio,
        /// que valida y descuenta el stock. El ViewModel no toca el inventario.
        /// </summary>
        private void ConfirmarVenta()
        {
            var venta = new Venta
            {
                IdCajero = UsuarioActual.IdUsuario,
                Cajero = UsuarioActual,
                IdCliente = ClienteSeleccionado?.IdCliente ?? 0,
                Cliente = ClienteSeleccionado!,
                IdMetodoPago = MetodoPagoSeleccionado?.IdMetodoPago ?? 0,
                MetodoPago = MetodoPagoSeleccionado!,
                ModalidadConsumo = EsConsumoLocal ? ModalidadConsumo.Local : ModalidadConsumo.ParaLlevar,
                IdUbicacion = EsConsumoLocal ? UbicacionSeleccionada?.IdUbicacion : null,
                Ubicacion = EsConsumoLocal ? UbicacionSeleccionada : null,
                IdMesero = RequiereMesero ? MeseroSeleccionado?.IdUsuario : null,
                Mesero = RequiereMesero ? MeseroSeleccionado : null
            };

            foreach (var linea in Ticket)
                venta.Detalles.Add(linea.ConvertirADetalle());

            var resultado = _repositorio.RegistrarVenta(venta);

            if (!resultado.Exito)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                return;
            }

            _dialogo.Informar("Venta confirmada", resultado.Mensaje);
            VaciarTicket();
            RefrescarIndicadores();
            MostrarMensaje(resultado.Mensaje, esError: false);
        }

        /// <summary>Recalcula disponibilidad, alertas y contador tras cada venta.</summary>
        private void RefrescarIndicadores()
        {
            foreach (var producto in Productos)
                producto.Disponibilidad = _repositorio.CalcularDisponibilidad(producto.Producto);

            AlertasStock.Clear();
            foreach (var alerta in _repositorio.ObtenerAlertasStock())
                AlertasStock.Add(alerta);

            VentasDelDia = _repositorio.ContarVentasDelDia();

            OnPropertyChanged(nameof(HayAlertasStock));
            OnPropertyChanged(nameof(TextoAlertas));
        }
    }
}
