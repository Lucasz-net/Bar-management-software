using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    public class CategoriaFiltroViewModel : ViewModelBase
    {
        public CategoriaFiltroViewModel(Categoria? categoria, string nombre, int cantidadProductos)
        {
            Categoria = categoria;
            Nombre = nombre;
            CantidadProductos = cantidadProductos;
        }

        public Categoria? Categoria { get; }

        public int? IdCategoria => Categoria?.IdCategoria;

        public string Nombre { get; }

        public int CantidadProductos { get; }

        public string TextoCantidad => CantidadProductos == 1 ? "1 producto" : $"{CantidadProductos} productos";

        private bool _estaSeleccionada;
        public bool EstaSeleccionada
        {
            get => _estaSeleccionada;
            set => SetProperty(ref _estaSeleccionada, value);
        }
    }
}
