using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// Base de todos los ViewModels. INotifyPropertyChanged es lo que hace que la UI
    /// se redibuje sola: el equivalente WPF del setState/signal de un framework web.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nombrePropiedad = null)
        {
            if (Equals(campo, valor))
                return false;

            campo = valor;
            OnPropertyChanged(nombrePropiedad);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? nombrePropiedad = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
    }
}
