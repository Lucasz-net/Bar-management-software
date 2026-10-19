using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// Lógica del modal de autorización. Pide credenciales y las valida contra el 
    /// repositorio para confirmar si el usuario tiene rol de Administrador o Gerente.
    /// </summary>
    public class AutorizacionViewModel : INotifyPropertyChanged
    {
        private readonly IRepositorioBar _repositorio;

        private string _emailSupervisor = string.Empty;
        private string _mensajeError = string.Empty;

        public event EventHandler? CierreSolicitado;
        public event PropertyChangedEventHandler? PropertyChanged;

        public AutorizacionViewModel(IRepositorioBar repositorio)
        {
            _repositorio = repositorio;

            ConfirmarCommand = new ComandoComun(EjecutarConfirmar, PuedeConfirmar);
            CancelarCommand = new ComandoComun(EjecutarCancelar);
        }

        public string EmailSupervisor
        {
            get => _emailSupervisor;
            set { _emailSupervisor = value; OnPropertyChanged(); }
        }

        public string MensajeError
        {
            get => _mensajeError;
            set { _mensajeError = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// El resultado final que leerá el servicio de diálogo cuando la ventana se cierre.
        /// </summary>
        public bool EstaAutorizado { get; private set; }

        public ICommand ConfirmarCommand { get; }
        public ICommand CancelarCommand { get; }

        private bool PuedeConfirmar(object? parametro)
        {
            return !string.IsNullOrWhiteSpace(EmailSupervisor);
        }

        private void EjecutarConfirmar(object? parametro)
        {
            // El PasswordBox no soporta binding, por lo que recibimos la vista directamente 
            // como parámetro para extraer la contraseña de forma segura.
            if (parametro is System.Windows.Controls.PasswordBox cajaClave)
            {
                string clave = cajaClave.Password;

                if (string.IsNullOrWhiteSpace(clave))
                {
                    MensajeError = "La contraseña es obligatoria.";
                    return;
                }

                var resultado = _repositorio.AutorizarSupervisor(EmailSupervisor, clave);

                if (resultado.Exito)
                {
                    EstaAutorizado = true;
                    MensajeError = string.Empty;
                    CierreSolicitado?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    EstaAutorizado = false;
                    MensajeError = resultado.Mensaje;
                    cajaClave.Clear(); // Limpiamos la clave si falló
                }
            }
        }

        private void EjecutarCancelar(object? parametro)
        {
            EstaAutorizado = false;
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }

        private void OnPropertyChanged([CallerMemberName] string? nombrePropiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
            (ConfirmarCommand as ComandoComun)?.NotificarCambio();
        }

        /// <summary>Implementación simple de ICommand para no depender de librerías externas.</summary>
        private class ComandoComun : ICommand
        {
            private readonly Action<object?> _ejecutar;
            private readonly Func<object?, bool>? _puedeEjecutar;

            public ComandoComun(Action<object?> ejecutar, Func<object?, bool>? puedeEjecutar = null)
            {
                _ejecutar = ejecutar;
                _puedeEjecutar = puedeEjecutar;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _puedeEjecutar is null || _puedeEjecutar(parameter);
            public void Execute(object? parameter) => _ejecutar(parameter);
            public void NotificarCambio() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}