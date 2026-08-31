using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _usuario = string.Empty;
        public string Usuario
        {
            get => _usuario;
            set => SetProperty(ref _usuario, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _mensaje = string.Empty;
        public string Mensaje
        {
            get => _mensaje;
            set => SetProperty(ref _mensaje, value);
        }

        private Brush _mensajeColor = Brushes.Red;
        public Brush MensajeColor
        {
            get => _mensajeColor;
            set => SetProperty(ref _mensajeColor, value);
        }

        public Usuario? UsuarioAutenticado { get; private set; }

        public event EventHandler? LoginExitoso;

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => Ingresar());
        }

        private void Ingresar()
        {
            if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
            {
                MensajeColor = Brushes.Red;
                Mensaje = "Complete todos los campos.";
                return;
            }

            var usuario = DatosPruebaUsuarios.ObtenerUsuariosMock()
                .FirstOrDefault(u =>
                    string.Equals(u.Correo, Usuario, StringComparison.OrdinalIgnoreCase) &&
                    u.Clave == Password);

            if (usuario != null)
            {
                UsuarioAutenticado = usuario;
                MensajeColor = Brushes.Green;
                Mensaje = "¡Inicio de sesión correcto!";
                LoginExitoso?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                MensajeColor = Brushes.Red;
                Mensaje = "Usuario o contraseña incorrectos.";
            }
        }
    }
}
