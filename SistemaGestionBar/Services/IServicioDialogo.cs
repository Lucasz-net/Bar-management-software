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

        void Informar(string titulo, string mensaje);

        bool Confirmar(string titulo, string mensaje);
    }
}
