using System;
using System.Windows.Input;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// ICommand genérico. Es lo que se bindea al Command de un Button:
    /// el equivalente WPF de pasarle un handler por props a un botón.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _ejecutar;
        private readonly Predicate<object?>? _puedeEjecutar;

        public RelayCommand(Action<object?> ejecutar, Predicate<object?>? puedeEjecutar = null)
        {
            _ejecutar = ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));
            _puedeEjecutar = puedeEjecutar;
        }

        public RelayCommand(Action ejecutar, Func<bool>? puedeEjecutar = null)
            : this(_ => ejecutar(), puedeEjecutar is null ? null : _ => puedeEjecutar())
        {
        }

        public bool CanExecute(object? parameter) => _puedeEjecutar?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _ejecutar(parameter);

        /// <summary>
        /// CommandManager reevalúa CanExecute solo ante cualquier interacción del usuario:
        /// alcanza para habilitar/deshabilitar botones sin notificar a mano.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    /// <summary>Variante tipada, para cuando el CommandParameter es un ViewModel concreto.</summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _ejecutar;
        private readonly Predicate<T>? _puedeEjecutar;

        public RelayCommand(Action<T> ejecutar, Predicate<T>? puedeEjecutar = null)
        {
            _ejecutar = ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));
            _puedeEjecutar = puedeEjecutar;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is not T tipado)
                return false;

            return _puedeEjecutar?.Invoke(tipado) ?? true;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T tipado)
                _ejecutar(tipado);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
