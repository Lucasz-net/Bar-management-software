using System.Linq;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// Envuelve un Producto para mostrarlo como tarjeta del catálogo.
    /// El Model queda limpio de estado de pantalla (cuánto lleva el ticket, si está sin stock):
    /// eso vive acá, igual que el estado local de un componente.
    /// </summary>
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

        /// <summary>RF-04: gobierna la visibilidad del botón "Ver Receta".</summary>
        public bool TieneReceta => Producto.TieneReceta;

        /// <summary>Ruta de la foto del producto. Null cuando todavía no tiene una cargada.</summary>
        public string? RutaImagen => Producto.RutaImagen;

        /// <summary>Si es false, la tarjeta cae al degradado con iniciales.</summary>
        public bool TieneImagen => !string.IsNullOrWhiteSpace(Producto.RutaImagen);

        /// <summary>Iniciales que se muestran cuando el producto no tiene foto.</summary>
        public string Iniciales =>
            string.Concat(Nombre.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                                .Take(2)
                                .Select(p => char.ToUpperInvariant(p[0])));

        private int _disponibilidad;
        /// <summary>Unidades vendibles hoy: stock propio o el que permitan los insumos (RF-07).</summary>
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

        /// <summary>Aviso visual en la tarjeta cuando quedan pocas unidades (RF-08).</summary>
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

        /// <summary>Filtro del catálogo: coincidencia por nombre o descripción.</summary>
        public bool CoincideConBusqueda(string texto) =>
            Nombre.Contains(texto, System.StringComparison.OrdinalIgnoreCase) ||
            Descripcion.Contains(texto, System.StringComparison.OrdinalIgnoreCase);
    }
}
