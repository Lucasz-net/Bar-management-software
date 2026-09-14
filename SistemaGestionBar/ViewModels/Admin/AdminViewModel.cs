using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Tablero del administrador. Es un segundo Shell, con la misma idea que MainWindow:
    /// un menú lateral elige la sección y un ContentControl la dibuja.
    ///
    /// Analogía web: MainViewModel es el router raíz (login / POS / admin) y este es un
    /// router anidado con sus propias rutas hijas.
    /// </summary>
    public class AdminViewModel : ViewModelBase
    {
        public AdminViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo, SesionActual sesion)
        {
            UsuarioActual = sesion.Usuario!;

            // RF-09: el rol decide qué secciones existen. No se ocultan en la vista,
            // directamente no se construyen: lo que no está no se puede alcanzar.
            //
            // Usuarios y Parámetros son configuración del sistema, no gestión del negocio,
            // así que quedan fuera del alcance del gerente.
            bool esAdministrador = UsuarioActual.EsAdministrador;

            Secciones = new ObservableCollection<SeccionAdminViewModel>();
            Secciones.Add(new AdminResumenViewModel(repositorio, dialogo));
            Secciones.Add(new AdminProductosViewModel(repositorio, dialogo));
            Secciones.Add(new AdminInventarioViewModel(repositorio, dialogo));

            // Personas y cuentas de acceso van juntas: son dos casilleros de la misma
            // ficha, y separarlas obligaba a ir y volver entre pantallas.
            if (esAdministrador)
                Secciones.Add(new AdminPersonalViewModel(repositorio, dialogo));

            Secciones.Add(new AdminVentasViewModel(repositorio, dialogo));

            // Reportes lo ven los dos roles: es información de gestión del negocio, que es
            // exactamente el alcance del gerente. Además no escribe nada —arma un PDF y lo
            // guarda en el disco del usuario—, así que no hay riesgo en dárselo.
            Secciones.Add(new AdminReportesViewModel(repositorio, dialogo, UsuarioActual));

            if (esAdministrador)
                Secciones.Add(new AdminParametrosViewModel(repositorio, dialogo));

            // Las secciones no se conocen entre sí: piden "llevame a Productos" y este
            // router resuelve si esa sección existe para el rol actual. Además se les
            // avisa qué destinos hay, para que no ofrezcan atajos a pantallas que este
            // usuario no tiene.
            var destinos = Secciones.Select(s => s.Destino).ToList();
            foreach (var seccion in Secciones)
            {
                seccion.NavegacionSolicitada += AlPedirNavegacion;
                seccion.ConfigurarDestinos(destinos);
            }

            SeleccionarSeccionCommand = new RelayCommand<SeccionAdminViewModel>(SeleccionarSeccion);
            CerrarSesionCommand = new RelayCommand(() => CierreSesionSolicitado?.Invoke(this, EventArgs.Empty));
            IrAlPuntoDeVentaCommand = new RelayCommand(() => PuntoDeVentaSolicitado?.Invoke(this, EventArgs.Empty));

            SeleccionarSeccion(Secciones[0]);
        }

        /// <summary>Vuelve al login (RF-09).</summary>
        public event EventHandler? CierreSesionSolicitado;

        /// <summary>El administrador también puede vender: esto lo lleva al POS.</summary>
        public event EventHandler? PuntoDeVentaSolicitado;

        public Usuario UsuarioActual { get; }

        public ObservableCollection<SeccionAdminViewModel> Secciones { get; }

        private SeccionAdminViewModel? _seccionActual;
        public SeccionAdminViewModel? SeccionActual
        {
            get => _seccionActual;
            private set => SetProperty(ref _seccionActual, value);
        }

        public string NombreUsuario => UsuarioActual.NombreCompleto;
        public string RolUsuario => UsuarioActual.NombreRol;

        /// <summary>Rótulo del menú lateral: el tablero es el mismo, el alcance no.</summary>
        public string TituloTablero => UsuarioActual.EsGerente ? "GERENCIA" : "ADMINISTRACIÓN";

        public string InicialesUsuario =>
            string.Concat(NombreUsuario.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                       .Take(2)
                                       .Select(p => char.ToUpperInvariant(p[0])));

        public string FechaActual
        {
            get
            {
                string fecha = DateTime.Now.ToString("dddd, dd 'de' MMMM", CultureInfo.CurrentCulture);
                return string.IsNullOrEmpty(fecha) ? fecha : char.ToUpper(fecha[0]) + fecha[1..];
            }
        }

        public ICommand SeleccionarSeccionCommand { get; }
        public ICommand CerrarSesionCommand { get; }
        public ICommand IrAlPuntoDeVentaCommand { get; }

        private void SeleccionarSeccion(SeccionAdminViewModel seccion)
        {
            foreach (var item in Secciones)
                item.EstaSeleccionada = ReferenceEquals(item, seccion);

            // Cada sección recarga al entrar: así los cambios hechos en otra
            // pantalla (o una venta nueva) se ven sin reiniciar la aplicación.
            seccion.Recargar();
            SeccionActual = seccion;
        }

        /// <summary>
        /// Resuelve un pedido de navegación hecho desde otra sección (las tarjetas del
        /// resumen, el botón Reponer de una alerta). Si el rol no tiene esa sección el
        /// pedido se ignora en silencio: lo que no está no se puede alcanzar (RF-09).
        /// </summary>
        private void AlPedirNavegacion(object? origen, PedidoDeNavegacion pedido)
        {
            var destino = Secciones.FirstOrDefault(s => s.Destino == pedido.Destino);
            if (destino is null)
                return;

            SeleccionarSeccion(destino);

            // Después de Recargar, para que la sección ya tenga sus listas cargadas
            // cuando busque el ítem que le señalaron.
            destino.Enfocar(pedido.Foco);
        }
    }
}
