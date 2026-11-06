using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaGestionBar.ViewModels
{
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
