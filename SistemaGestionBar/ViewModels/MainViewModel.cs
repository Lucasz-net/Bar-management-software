using SistemaGestionBar.Models;
using SistemaGestionBar.Views;

namespace SistemaGestionBar.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView = null!;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public MainViewModel()
        {
            MostrarLogin();
        }

        private void MostrarLogin()
        {
            var loginViewModel = new LoginViewModel();
            loginViewModel.LoginExitoso += (s, e) =>
            {
                if (loginViewModel.UsuarioAutenticado != null)
                {
                    MostrarDashboard(loginViewModel.UsuarioAutenticado);
                }
            };

            CurrentView = new LoginView { DataContext = loginViewModel };
        }

        private void MostrarDashboard(Usuario usuario)
        {
            CurrentView = new Dashboard(usuario);
        }
    }
}
