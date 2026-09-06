using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Producto. Ítem vendible del catálogo.
    /// Si tiene filas en Producto_Ingrediente se prepara con receta y su disponibilidad
    /// sale de los insumos; si no, es venta directa y usa su propio campo Stock (RF-07).
    /// </summary>
    public class Producto : EntidadAuditable
    {
        public int IdProducto { get; set; }
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }

        /// <summary>
        /// EXTENSIÓN AL DER: umbral de reposición para los productos de venta directa (RF-08).
        /// Ingrediente ya tiene su stock_minimo; Producto no lo tenía, así que las alertas
        /// de las botellas usaban un número fijo en el código. Con esta columna cada producto
        /// define el suyo. Los productos con receta lo dejan en 0: su disponibilidad
        /// depende de los insumos, no de este campo.
        /// Propuesta: columna stock_minimo INT NOT NULL DEFAULT 0 en Producto.
        /// </summary>
        public int StockMinimo { get; set; }

        /// <summary>
        /// EXTENSIÓN AL DER: foto del producto para el catálogo del punto de venta.
        /// El esquema original no tiene columna de imagen. Se guarda la RUTA, no el binario:
        /// meter fotos como VARBINARY en la tabla infla la base y hace lento cada SELECT.
        /// Propuesta: columna ruta_imagen VARCHAR(260) en Producto.
        /// </summary>
        public string? RutaImagen { get; set; }

        // Navegación
        public Categoria Categoria { get; set; } = null!;
        public ICollection<ProductoIngrediente> Ingredientes { get; set; } = new List<ProductoIngrediente>();

        /// <summary>
        /// Null para lo que se vende cerrado (una botella no se prepara).
        /// Solo trae el procedimiento; los ingredientes están en <see cref="Ingredientes"/>.
        /// </summary>
        public Receta? Receta { get; set; }

        /// <summary>
        /// RF-04: el botón "Ver Receta" solo aparece cuando esto es true.
        /// La condición la manda Producto_Ingrediente, tal como pide el requerimiento.
        /// </summary>
        public bool TieneReceta => Ingredientes.Any();

        /// <summary>
        /// RF-08 para venta directa. Los productos con receta quedan fuera: sus alertas
        /// salen del stock de los ingredientes.
        /// </summary>
        public bool StockBajo => !TieneReceta && StockMinimo > 0 && Stock <= StockMinimo;
    }
}
