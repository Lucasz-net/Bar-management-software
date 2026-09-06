using System.Collections.Generic;
using System.Linq;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// RF-04. Datos del modal de receta. Se arma de dos tablas:
    /// los ingredientes con sus cantidades salen de Producto_Ingrediente, y el
    /// procedimiento paso a paso de la tabla Receta.
    /// Solo se instancia para productos que se preparan.
    /// </summary>
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

            // Un producto puede tener ingredientes cargados y todavía no tener
            // escrito el procedimiento: la fila de Receta es opcional.
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
