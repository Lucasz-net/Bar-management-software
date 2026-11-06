using System.Collections.Generic;
using System.Linq;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    public class RecetaViewModel : ViewModelBase
    {
        public RecetaViewModel(Producto producto)
        {
            NombreProducto = producto.Nombre;
            Categoria = producto.Categoria?.NombreCategoria ?? string.Empty;
            Descripcion = producto.Descripcion ?? string.Empty;

            Ingredientes = producto.Ingredientes
                .OrderByDescending(i => i.CantidadNecesaria)
                .Select(i => i.Descripcion)
                .ToList();

            Instrucciones = string.IsNullOrWhiteSpace(producto.Receta?.Instrucciones)
                ? "Este producto todavía no tiene el paso a paso cargado."
                : producto.Receta!.Instrucciones;
        }

        public string NombreProducto { get; }
        public string Categoria { get; }
        public string Descripcion { get; }
        public IReadOnlyList<string> Ingredientes { get; }
        public string Instrucciones { get; }
    }
}
