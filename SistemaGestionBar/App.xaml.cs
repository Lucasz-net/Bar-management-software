using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using SistemaGestionBar.Data;
using SistemaGestionBar.Services;

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

            // La base se prepara ANTES de abrir la ventana. Si algo falla —MySQL apagado,
            // falta el archivo de conexión, la clave cambió— hay que decirlo con un
            // cartel que explique qué hacer, y no dejar que reviente con un
            // XamlParseException que no le sirve a nadie.
            if (!PrepararBaseDeDatos())
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        private static bool PrepararBaseDeDatos()
        {
            try
            {
                SembradorDeDatos.Preparar(new FabricaDeContexto(new SesionActual()));
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo abrir la base de datos.\n\n" +
                    (ex.InnerException ?? ex).Message + "\n\n" +
                    "Cosas para revisar:\n" +
                    "  · Que el servicio de MySQL esté corriendo.\n" +
                    $"  · Que exista «{CadenaDeConexion.ArchivoLocal}» junto al proyecto,\n" +
                    $"    copiado de «{CadenaDeConexion.ArchivoEjemplo}» y con tus datos.\n" +
                    "  · Que el usuario y la contraseña de ese archivo sean los correctos.",
                    "Sistema de Gestión de Bar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }
    }
}
