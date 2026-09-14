using SistemaGestionBar.Data;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;
using SistemaGestionBar.ViewModels.Admin;

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
        private readonly SesionActual _sesion;

        public MainViewModel() : this(new SesionActual())
        {
        }

        /// <summary>
        /// El arranque real de la aplicación: se conecta a MySQL, aplica las migraciones
        /// que falten y siembra los datos si la base está vacía. Recién entonces se
        /// construye el repositorio que van a usar todas las pantallas.
        /// </summary>
        private MainViewModel(SesionActual sesion) : this(Conectar(sesion), new ServicioDialogo(), sesion)
        {
        }

        private static IRepositorioBar Conectar(SesionActual sesion)
        {
            var fabrica = new FabricaDeContexto(sesion);
            SembradorDeDatos.Preparar(fabrica);
            return new RepositorioSql(fabrica, sesion);
        }

        public MainViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo, SesionActual sesion)
        {
            _repositorio = repositorio;
            _dialogo = dialogo;
            _sesion = sesion;
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
            _sesion.Usuario = null;

            var login = new LoginViewModel(_repositorio);
            login.LoginExitoso += (_, usuario) =>
            {
                _sesion.Usuario = usuario;

                // RF-09: el rol decide con qué pantalla arranca cada uno.
                if (usuario.AccedeAlTablero)
                    MostrarTablero();
                else
                    MostrarPuntoDeVenta(usuario);
            };

            VistaActual = login;
        }

        private void MostrarTablero()
        {
            var admin = new AdminViewModel(_repositorio, _dialogo, _sesion);
            admin.CierreSesionSolicitado += (_, _) => MostrarLogin();
            admin.PuntoDeVentaSolicitado += (_, _) => MostrarPuntoDeVenta(_sesion.Usuario!);
            VistaActual = admin;
        }

        private void MostrarPuntoDeVenta(Usuario usuario)
        {
            var puntoDeVenta = new PuntoDeVentaViewModel(_repositorio, _dialogo, usuario);

            // Dos salidas distintas y explícitas: cerrar sesión siempre sale al login,
            // y el administrador tiene además un botón propio para volver a su tablero.
            puntoDeVenta.CierreSesionSolicitado += (_, _) => MostrarLogin();
            puntoDeVenta.TableroSolicitado += (_, _) => MostrarTablero();

            VistaActual = puntoDeVenta;
        }
    }
}
