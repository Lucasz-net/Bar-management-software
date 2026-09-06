using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// Tarjeta de categoría del catálogo. La cantidad de productos se calcula
    /// de los datos reales, ya no es un número escrito a mano en el XAML.
    /// </summary>
    public class CategoriaFiltroViewModel : ViewModelBase
    {
        public CategoriaFiltroViewModel(Categoria? categoria, string nombre, int cantidadProductos)
        {
            Categoria = categoria;
            Nombre = nombre;
            CantidadProductos = cantidadProductos;
        }

        /// <summary>null representa la pseudo-categoría "Todos".</summary>
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
