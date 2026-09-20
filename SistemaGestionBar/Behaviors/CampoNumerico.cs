using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SistemaGestionBar.Behaviors
{
    /// <summary>
    /// Hace que un <see cref="TextBox"/> acepte únicamente números.
    ///
    /// Lo usa el campo del DNI. La validación ya rechaza cualquier otra cosa, pero
    /// rechazar no es lo mismo que impedir: si el campo deja escribir "38.987.654" y
    /// recién después lo pinta de rojo, el usuario ya escribió el documento entero antes
    /// de enterarse de cómo había que cargarlo. Impedirlo enseña la regla sin un mensaje.
    ///
    /// Es un asunto de la vista —qué teclas admite un control— así que vive acá y no en
    /// el ViewModel.
    /// </summary>
    public static class CampoNumerico
    {
        public static readonly DependencyProperty SoloDigitosProperty =
            DependencyProperty.RegisterAttached(
                "SoloDigitos",
                typeof(bool),
                typeof(CampoNumerico),
                new PropertyMetadata(false, AlCambiarSoloDigitos));

        public static bool GetSoloDigitos(DependencyObject objeto) =>
            (bool)objeto.GetValue(SoloDigitosProperty);

        public static void SetSoloDigitos(DependencyObject objeto, bool valor) =>
            objeto.SetValue(SoloDigitosProperty, valor);

        private static void AlCambiarSoloDigitos(DependencyObject objeto, DependencyPropertyChangedEventArgs e)
        {
            if (objeto is not TextBox caja)
                return;

            caja.PreviewTextInput -= AlEscribir;
            caja.PreviewKeyDown -= AlApretarTecla;
            DataObject.RemovePastingHandler(caja, AlPegar);

            if (!(bool)e.NewValue)
                return;

            caja.PreviewTextInput += AlEscribir;
            caja.PreviewKeyDown += AlApretarTecla;
            DataObject.AddPastingHandler(caja, AlPegar);
        }

        private static void AlEscribir(object remitente, TextCompositionEventArgs e) =>
            e.Handled = !e.Text.All(char.IsDigit);

        /// <summary>
        /// La barra espaciadora no pasa por PreviewTextInput en todos los casos, así que
        /// se ataja aparte.
        /// </summary>
        private static void AlApretarTecla(object remitente, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        /// <summary>
        /// Al pegar no se rechaza: se limpia. Alguien que copia "38.987.654" de una
        /// planilla quiere pegar ese documento, no pelearse con el campo.
        /// </summary>
        private static void AlPegar(object remitente, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true))
            {
                e.CancelCommand();
                return;
            }

            string pegado = (string)e.SourceDataObject.GetData(DataFormats.UnicodeText, true) ?? string.Empty;
            string numeros = new(pegado.Where(char.IsDigit).ToArray());

            if (numeros.Length == 0)
            {
                e.CancelCommand();
                return;
            }

            if (numeros == pegado)
                return;

            var limpio = new DataObject();
            limpio.SetData(DataFormats.UnicodeText, numeros);
            e.DataObject = limpio;
        }
    }
}
