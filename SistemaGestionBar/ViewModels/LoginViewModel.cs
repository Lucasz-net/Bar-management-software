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

        /// <summary>El CORREO DE TRABAJO, que es el nombre de usuario (RF-09).</summary>
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

        /// <summary>
        /// Al empezar a corregir se borra el cartel rojo del intento anterior. Dejarlo
        /// puesto mientras se reescribe la contraseña es decirle al usuario que está
        /// equivocado justo mientras se corrige.
        /// </summary>
        private void OlvidarElIntentoAnterior()
        {
            if (MensajeEsError && HayMensaje)
                Mensaje = string.Empty;
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
                // La clave se borra ANTES de poner el cartel: hacerlo después dispararía
                // OlvidarElIntentoAnterior y el mensaje no llegaría a verse nunca.
                Clave = string.Empty;

                MensajeEsError = true;
                Mensaje = "Correo o contraseña incorrectos.";
                return;
            }

            MensajeEsError = false;
            Mensaje = $"Bienvenido, {usuario.NombreCompleto}.";
            LoginExitoso?.Invoke(this, usuario);
        }
    }
}
