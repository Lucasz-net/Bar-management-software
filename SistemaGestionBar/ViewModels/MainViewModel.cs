using SistemaGestionBar.Data;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;
using SistemaGestionBar.ViewModels.Admin;

namespace SistemaGestionBar.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IRepositorioBar _repositorio;
        private readonly IServicioDialogo _dialogo;
        private readonly SesionActual _sesion;

        public MainViewModel() : this(new SesionActual())
        {
        }

        private MainViewModel(SesionActual sesion) : this(Conectar(sesion), new ServicioDialogo(), sesion)
        {
        }

        private static IRepositorioBar Conectar(SesionActual sesion)
        {
            var fabrica = new FabricaDeContexto(sesion);
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

            puntoDeVenta.CierreSesionSolicitado += (_, _) => MostrarLogin();
            puntoDeVenta.TableroSolicitado += (_, _) => MostrarTablero();

            VistaActual = puntoDeVenta;
        }
    }
}
