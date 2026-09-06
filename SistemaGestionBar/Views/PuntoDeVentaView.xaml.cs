using System.Windows.Controls;

namespace SistemaGestionBar.Views
{
    /// <summary>
    /// Punto de venta. Sin logica de negocio: todo el comportamiento vive en
    /// PuntoDeVentaViewModel y llega por bindings y comandos.
    /// </summary>
    public partial class PuntoDeVentaView : UserControl
    {
        public PuntoDeVentaView()
        {
            InitializeComponent();
        }
    }
}
