namespace SistemaGestionBar.Models
{
    /// <summary>
    /// EXTENSIÓN AL DER: tabla Receta. Guarda el procedimiento paso a paso que pide RF-04.
    ///
    /// Va en tabla aparte y no como columna de Producto porque solo tiene sentido para lo que
    /// se prepara: una botella de vino o un agua mineral no tienen procedimiento, y la columna
    /// quedaría en NULL para más de la mitad del catálogo. Así, existir una fila ES el dato:
    /// hay receta escrita para este producto.
    ///
    /// Relación 1-1 opcional con Producto: id_producto es a la vez clave primaria y foránea,
    /// lo que impide que un producto tenga dos recetas.
    ///
    /// Ojo: los INGREDIENTES siguen en Producto_Ingrediente, tal como define el DER.
    /// Esta tabla solo agrega el texto del procedimiento.
    /// </summary>
    public class Receta : EntidadAuditable
    {
        /// <summary>Clave primaria y foránea a la vez (1-1 con Producto).</summary>
        public int IdProducto { get; set; }

        /// <summary>Procedimiento paso a paso, un paso por línea. Columna instrucciones.</summary>
        public string Instrucciones { get; set; } = string.Empty;

        public Producto Producto { get; set; } = null!;
    }
}
