using System;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// RF-09. Valida email (de Persona) + clave hasheada (de Usuario) contra el repositorio.
    /// El ViewModel no sabe cómo se hashea ni de dónde salen los usuarios.
    /// </summary>
    public class LoginViewModel : ViewModelBase
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
            set => SetProperty(ref _email, value);
        }

        private string _clave = string.Empty;
        public string Clave
        {
            get => _clave;
            set => SetProperty(ref _clave, value);
        }

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

        private bool _mensajeEsError = true;
        /// <summary>
        /// El color lo decide la View con un DataTrigger. Un ViewModel que expusiera
        /// un Brush estaría mezclando presentación con lógica.
        /// </summary>
        public bool MensajeEsError
        {
            get => _mensajeEsError;
            private set => SetProperty(ref _mensajeEsError, value);
        }

        public bool HayMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        public ICommand IngresarCommand { get; }

        public event EventHandler<Usuario>? LoginExitoso;

        private bool PuedeIngresar() =>
            !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Clave);

        private void Ingresar()
        {
            var usuario = _repositorio.Autenticar(Email.Trim(), Clave);

            if (usuario is null)
            {
                MensajeEsError = true;
                Mensaje = "Usuario o contraseña incorrectos.";
                Clave = string.Empty;
                return;
            }

            MensajeEsError = false;
            Mensaje = $"Bienvenido, {usuario.NombreCompleto}.";
            LoginExitoso?.Invoke(this, usuario);
        }
    }
}
