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

            if (esAdministrador)
                Secciones.Add(new AdminUsuariosViewModel(repositorio, dialogo));

            Secciones.Add(new AdminVentasViewModel(repositorio, dialogo));

            if (esAdministrador)
                Secciones.Add(new AdminParametrosViewModel(repositorio, dialogo));

            Secciones.Add(new AdminReportesViewModel(repositorio, dialogo, sesion));

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
    }
}
