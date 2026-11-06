using System;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    public class LoginViewModel : ViewModelConMensaje
    {
        private readonly IRepositorioBar _repositorio;

        public LoginViewModel(IRepositorioBar repositorio)
        {
            _repositorio = repositorio;
            IngresarCommand = new RelayCommand(Ingresar, PuedeIngresar);
        }

        private string _email = string.Empty;

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                    OlvidarElIntentoAnterior();
            }
        }

        private string _clave = string.Empty;
        public string Clave
        {
            get => _clave;
            set
            {
                if (SetProperty(ref _clave, value))
                    OlvidarElIntentoAnterior();
            }
        }

        private void OlvidarElIntentoAnterior()
        {
            if (MensajeEsError && HayMensaje)
                LimpiarMensaje();
        }

        public ICommand IngresarCommand { get; }

        public event EventHandler<Usuario>? LoginExitoso;

        private bool PuedeIngresar() =>
            !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Clave);

        private void Ingresar()
        {
            var usuario = _repositorio.Autenticar(Email.Trim(), Clave);

            if (usuario is null)
            {
                Clave = string.Empty;
                Fallar("Correo o contraseña incorrectos.");
                return;
            }

            Informar($"Bienvenido, {usuario.NombreCompleto}.");
            LoginExitoso?.Invoke(this, usuario);
        }
    }
}
