using SistemaGestionBar.Data;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// El "router" de la aplicación. MainWindow es solo un cascarón con un ContentControl
    /// bindeado a VistaActual; acá se decide qué ViewModel ocupa esa ranura y App.xaml
    /// define, con DataTemplates, qué UserControl dibuja cada uno.
    ///
    /// Analogía web: VistaActual es la ruta activa y los DataTemplates son el mapa
    /// ruta -> componente. Por eso navegamos apuntando a ViewModels y no a Views.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IRepositorioBar _repositorio;
        private readonly IServicioDialogo _dialogo;

        public MainViewModel() : this(new RepositorioMemoria(), new ServicioDialogo())
        {
        }

        public MainViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
        {
            _repositorio = repositorio;
            _dialogo = dialogo;
            MostrarLogin();
        }

        private object? _vistaActual;
        public object? VistaActual
        {
            get => _vistaActual;
            private set => SetProperty(ref _vistaActual, value);
        }

        private void MostrarLogin()
        {
            var login = new LoginViewModel(_repositorio);
            login.LoginExitoso += (_, usuario) => MostrarPuntoDeVenta(usuario);
            VistaActual = login;
        }

        private void MostrarPuntoDeVenta(Usuario usuario)
        {
            var puntoDeVenta = new PuntoDeVentaViewModel(_repositorio, _dialogo, usuario);
            puntoDeVenta.CierreSesionSolicitado += (_, _) => MostrarLogin();
            VistaActual = puntoDeVenta;
        }
    }
}
