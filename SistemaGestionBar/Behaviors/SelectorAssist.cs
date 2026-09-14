using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SistemaGestionBar.Behaviors
{
    /// <summary>
    /// Dos comportamientos adjuntos sobre las listas del tablero: desplazarse hasta la
    /// fila seleccionada, y congelar la selección mientras hay un formulario abierto.
    ///
    /// Los dos son asuntos de la vista —qué se ve y qué se puede tocar— así que viven
    /// acá y no en el ViewModel.
    /// </summary>
    public static class SelectorAssist
    {
        // ===============================================================
        // Desplazarse hasta la selección
        // ===============================================================
        /// <summary>
        /// Hace que la lista se desplace hasta la fila seleccionada cuando la selección la
        /// cambia el ViewModel y no el usuario.
        ///
        /// Hace falta porque WPF solo desplaza cuando el clic viene del mouse o del teclado:
        /// si el resumen manda "reponé Triple Sec", la fila queda seleccionada pero fuera de
        /// la pantalla y parece que no pasó nada.
        ///
        /// Analogía web: es el scrollIntoView() que uno llama después de cambiar el estado.
        /// </summary>
        public static readonly DependencyProperty DesplazarASeleccionProperty =
            DependencyProperty.RegisterAttached(
                "DesplazarASeleccion",
                typeof(bool),
                typeof(SelectorAssist),
                new PropertyMetadata(false, AlCambiarElDesplazamiento));

        public static bool GetDesplazarASeleccion(DependencyObject objeto) =>
            (bool)objeto.GetValue(DesplazarASeleccionProperty);

        public static void SetDesplazarASeleccion(DependencyObject objeto, bool valor) =>
            objeto.SetValue(DesplazarASeleccionProperty, valor);

        private static void AlCambiarElDesplazamiento(DependencyObject objeto, DependencyPropertyChangedEventArgs e)
        {
            if (objeto is not Selector selector)
                return;

            selector.SelectionChanged -= AlCambiarLaSeleccion;

            if (e.NewValue is true)
                selector.SelectionChanged += AlCambiarLaSeleccion;
        }

        private static void AlCambiarLaSeleccion(object remitente, SelectionChangedEventArgs e)
        {
            if (remitente is not Selector selector || selector.SelectedItem is null)
                return;

            // En cola y no ahora mismo: si la selección cambió durante una recarga, los
            // contenedores de las filas todavía no existen y ScrollIntoView no encuentra
            // a dónde ir.
            selector.Dispatcher.BeginInvoke(new Action(() =>
            {
                object? item = selector.SelectedItem;
                if (item is null)
                    return;

                switch (selector)
                {
                    case DataGrid grilla:
                        // Con la primera columna como destino: ScrollIntoView(item) a secas
                        // lleva a la vista la celda actual, que puede estar a la derecha, y
                        // deja la grilla desplazada en horizontal mostrando las columnas del
                        // medio. Acá siempre queremos ver la fila desde el principio.
                        grilla.ScrollIntoView(item, grilla.Columns.FirstOrDefault());
                        break;
                    case ListBox lista:
                        lista.ScrollIntoView(item);
                        break;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        // ===============================================================
        // Congelar la selección mientras se edita
        // ===============================================================
        /// <summary>
        /// Mientras esto sea true, la lista no cambia de fila seleccionada: el marco de
        /// selección se queda donde está.
        ///
        /// <b>Por qué no alcanza con rechazarlo en el ViewModel.</b> El setter puede
        /// ignorar el valor nuevo y volver a levantar <c>PropertyChanged</c>, pero ese
        /// aviso llega <i>mientras</i> el binding está transfiriendo del control al
        /// origen, y WPF lo descarta para no entrar en bucle: el ViewModel queda en su
        /// producto y la lista se queda con el otro remarcado. La única forma de que el
        /// marco azul no se mueva es que el clic no llegue nunca a cambiar la selección.
        ///
        /// Se bloquea solo lo que selecciona: los clics sobre una fila y las teclas de
        /// navegación. La barra de desplazamiento y la rueda del mouse siguen andando,
        /// porque mirar la lista mientras se edita es legítimo.
        /// </summary>
        public static readonly DependencyProperty SeleccionBloqueadaProperty =
            DependencyProperty.RegisterAttached(
                "SeleccionBloqueada",
                typeof(bool),
                typeof(SelectorAssist),
                new PropertyMetadata(false, AlCambiarElBloqueo));

        public static bool GetSeleccionBloqueada(DependencyObject objeto) =>
            (bool)objeto.GetValue(SeleccionBloqueadaProperty);

        public static void SetSeleccionBloqueada(DependencyObject objeto, bool valor) =>
            objeto.SetValue(SeleccionBloqueadaProperty, valor);

        private static void AlCambiarElBloqueo(DependencyObject objeto, DependencyPropertyChangedEventArgs e)
        {
            if (objeto is not Selector selector)
                return;

            selector.PreviewMouseLeftButtonDown -= AlIntentarSeleccionarConElMouse;
            selector.PreviewKeyDown -= AlIntentarSeleccionarConElTeclado;

            if (e.NewValue is not true)
                return;

            selector.PreviewMouseLeftButtonDown += AlIntentarSeleccionarConElMouse;
            selector.PreviewKeyDown += AlIntentarSeleccionarConElTeclado;
        }

        private static void AlIntentarSeleccionarConElMouse(object remitente, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject origen && EsUnaFila(origen))
                e.Handled = true;
        }

        private static void AlIntentarSeleccionarConElTeclado(object remitente, KeyEventArgs e)
        {
            e.Handled = e.Key is Key.Up or Key.Down or Key.Left or Key.Right
                              or Key.Home or Key.End or Key.PageUp or Key.PageDown
                              or Key.Space;
        }

        /// <summary>
        /// ¿El clic cayó sobre una fila? Si cayó sobre la barra de desplazamiento se deja
        /// pasar: esa no selecciona nada.
        /// </summary>
        private static bool EsUnaFila(DependencyObject origen)
        {
            for (var actual = origen; actual is not null; actual = Padre(actual))
            {
                if (actual is ScrollBar)
                    return false;

                if (actual is ListBoxItem or DataGridRow or DataGridCell)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// El OriginalSource de un clic puede ser un elemento que no está en el árbol
        /// visual (un Run adentro de un TextBlock, por ejemplo), y ahí VisualTreeHelper
        /// tira excepción. Por eso se elige el árbol según el tipo.
        /// </summary>
        private static DependencyObject? Padre(DependencyObject objeto) =>
            objeto is Visual or Visual3D
                ? VisualTreeHelper.GetParent(objeto)
                : LogicalTreeHelper.GetParent(objeto);
    }
}
