using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace SistemaGestionBar
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Cultura es-AR para que los precios salgan con formato de pesos y las
            // fechas en espanol. El OverrideMetadata es el que hace que los bindings
            // del XAML (StringFormat) usen esta cultura y no en-US por defecto.
            var cultura = new CultureInfo("es-AR");
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;
            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));

            base.OnStartup(e);
        }
    }
}
