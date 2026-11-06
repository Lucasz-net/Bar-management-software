using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminViewModel : ViewModelBase
    {
        public AdminViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo, SesionActual sesion)
        {
            UsuarioActual = sesion.Usuario!;

            bool esAdministrador = UsuarioActual.EsAdministrador;
            bool esGerente = UsuarioActual.EsGerente;

            Secciones = new ObservableCollection<SeccionAdminViewModel>();

            if (esAdministrador)
                Secciones.Add(new AdminResumenViewModel(repositorio, dialogo));

            if (esGerente)
                Secciones.Add(new GestionViewModel(repositorio, dialogo));

            Secciones.Add(new AdminProductosViewModel(repositorio, dialogo));
            Secciones.Add(new AdminInventarioViewModel(repositorio, dialogo));
            Secciones.Add(new AdminPersonalViewModel(repositorio, dialogo));

            if (esAdministrador)
                Secciones.Add(new AdminUsuariosViewModel(repositorio, dialogo));

            if (esGerente)
                Secciones.Add(new EquipoViewModel(repositorio, dialogo));

            Secciones.Add(new AdminVentasViewModel(repositorio, dialogo));
            Secciones.Add(new AdminReportesViewModel(repositorio, dialogo, UsuarioActual));
            Secciones.Add(new AdminParametrosViewModel(repositorio, dialogo));

            if (esAdministrador || esGerente)
                Secciones.Add(new AdminBackupsViewModel(repositorio, dialogo, UsuarioActual));

            var destinos = Secciones.Select(s => s.Destino).ToList();
            foreach (var seccion in Secciones)
            {
                seccion.NavegacionSolicitada += AlPedirNavegacion;
                seccion.ConfigurarDestinos(destinos);
            }

            SeleccionarSeccionCommand = new RelayCommand<SeccionAdminViewModel>(SeleccionarSeccion);
            CerrarSesionCommand = new RelayCommand(() => CierreSesionSolicitado?.Invoke(this, EventArgs.Empty));
            IrAlPuntoDeVentaCommand = new RelayCommand(() => PuntoDeVentaSolicitado?.Invoke(this, EventArgs.Empty));
            AlternarMenuCommand = new RelayCommand(() => MenuExpandido = !MenuExpandido);

            SeleccionarSeccion(Secciones[0]);
        }

        public event EventHandler? CierreSesionSolicitado;
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
        public ICommand AlternarMenuCommand { get; }

        private bool _menuExpandido = true;

        public bool MenuExpandido
        {
            get => _menuExpandido;
            set => SetProperty(ref _menuExpandido, value);
        }

        private void SeleccionarSeccion(SeccionAdminViewModel seccion)
        {
            foreach (var item in Secciones)
                item.EstaSeleccionada = ReferenceEquals(item, seccion);

            seccion.Recargar();
            SeccionActual = seccion;
        }

        private void AlPedirNavegacion(object? origen, PedidoDeNavegacion pedido)
        {
            var destino = Secciones.FirstOrDefault(s => s.Destino == pedido.Destino);
            if (destino is null)
                return;

            SeleccionarSeccion(destino);
            destino.Enfocar(pedido.Foco);
        }
    }
}