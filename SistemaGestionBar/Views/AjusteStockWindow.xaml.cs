using System.Windows;

namespace SistemaGestionBar.Views
{
    /// <summary>
    /// Modal de ajuste de stock (RF-08). Lo abre ServicioDialogo con un
    /// AjusteDeStockViewModel como DataContext. Cancelar cierra por IsCancel; el cierre al
    /// guardar lo hace ServicioDialogo escuchando CierreSolicitado, asi que aca no hay logica.
    /// </summary>
    public partial class AjusteStockWindow : Window
    {
        public AjusteStockWindow()
        {
            InitializeComponent();
        }
    }
}
