using SistemaGestionBar.ViewModels;

namespace SistemaGestionBar.Services
{
    /// <summary>
    /// Abstrae "abrir ventanas" para que los ViewModels no dependan de WPF.
    /// Sin esto, un ViewModel tendría que hacer new RecetaWindow().ShowDialog() y
    /// dejaría de ser testeable. Analogía web: un servicio de modales inyectado
    /// en el componente en lugar de tocar el DOM directamente.
    /// </summary>
    public interface IServicioDialogo
    {
        /// <summary>RF-04: abre el modal de receta.</summary>
        void MostrarReceta(RecetaViewModel receta);

        void MostrarFactura(ViewModels.FacturaViewModel factura);

        /// <summary>
        /// RF-08: abre la ventanita que modifica el stock de un ítem, y solo el stock.
        /// Devuelve true si el ajuste llegó a guardarse, para que la pantalla que la abrió
        /// recargue su lista y muestre el resultado.
        /// </summary>
        bool AjustarStock(ViewModels.AjusteDeStockViewModel ajuste);

        void Informar(string titulo, string mensaje);

        bool Confirmar(string titulo, string mensaje);

        /// <summary>
        /// Pregunta al usuario dónde guardar un PDF. Devuelve la ruta elegida, o null si
        /// cerró el cuadro sin elegir.
        ///
        /// Va acá y no en el ViewModel por la misma razón que el resto: SaveFileDialog es
        /// WPF, y el ViewModel no debe conocer WPF.
        /// </summary>
        string? ElegirDondeGuardarPdf(string nombreSugerido);

        /// <summary>Abre un archivo con el programa que el sistema tenga asociado.</summary>
        void AbrirArchivo(string ruta);
    }
}
