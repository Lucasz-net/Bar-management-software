using System.Linq;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    public class ProductoCatalogoViewModel : ViewModelBase
    {
        public ProductoCatalogoViewModel(Producto producto, int disponibilidad)
        {
            Producto = producto;
            _disponibilidad = disponibilidad;
        }

        public Producto Producto { get; }

        public int IdProducto => Producto.IdProducto;
        public int IdCategoria => Producto.IdCategoria;
        public string Nombre => Producto.Nombre;
        public string Descripcion => Producto.Descripcion ?? string.Empty;
        public decimal Precio => Producto.Precio;
        public string NombreCategoria => Producto.Categoria?.NombreCategoria ?? string.Empty;

        public bool TieneReceta => Producto.TieneReceta;

        public string? RutaImagen => Producto.RutaImagen;

        public bool TieneImagen => !string.IsNullOrWhiteSpace(Producto.RutaImagen);

        public string Iniciales =>
            string.Concat(Nombre.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                                .Take(2)
                                .Select(p => char.ToUpperInvariant(p[0])));

        private int _disponibilidad;
        public int Disponibilidad
        {
            get => _disponibilidad;
            set
            {
                if (SetProperty(ref _disponibilidad, value))
                {
                    OnPropertyChanged(nameof(SinStock));
                    OnPropertyChanged(nameof(StockBajo));
                    OnPropertyChanged(nameof(TextoStock));
                }
            }
        }

        public bool SinStock => Disponibilidad <= 0;

        public bool StockBajo => Disponibilidad > 0 && Disponibilidad <= 5;

        public string TextoStock => SinStock ? "Sin stock" : $"{Disponibilidad} disp.";

        private int _cantidadEnTicket;
        public int CantidadEnTicket
        {
            get => _cantidadEnTicket;
            set
            {
                if (SetProperty(ref _cantidadEnTicket, value))
                    OnPropertyChanged(nameof(EstaEnTicket));
            }
        }

        public bool EstaEnTicket => CantidadEnTicket > 0;

        public bool CoincideConBusqueda(string texto) =>
            Nombre.Contains(texto, System.StringComparison.OrdinalIgnoreCase) ||
            Descripcion.Contains(texto, System.StringComparison.OrdinalIgnoreCase);
    }
}
