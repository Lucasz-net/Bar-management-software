using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SistemaGestionBar.Reportes;

namespace SistemaGestionBar.Behaviors
{
    /// <summary>
    /// Dibuja la vista previa de un <see cref="BloqueTabla"/> dentro de un ContentControl.
    ///
    /// <b>Por qué no es XAML.</b> Las columnas de un reporte no se conocen hasta que el
    /// reporte se arma: el de ventas tiene seis y el de insumos, otras seis distintas. Un
    /// DataGrid con columnas declaradas en el XAML no puede representar eso, y el único
    /// modo de que las celdas queden alineadas en columnas es construir la grilla con los
    /// anchos que trae el bloque. Eso es lo que hace esta clase.
    ///
    /// Sigue siendo MVVM: el ViewModel no conoce WPF —entrega el bloque y nada más— y la
    /// vista no tiene code-behind. Es el mismo patrón que <see cref="SelectorAssist"/>.
    /// </summary>
    public static class TablaDeReporteAssist
    {
        public static readonly DependencyProperty BloqueProperty =
            DependencyProperty.RegisterAttached(
                "Bloque",
                typeof(BloqueTabla),
                typeof(TablaDeReporteAssist),
                new PropertyMetadata(null, AlCambiarElBloque));

        public static void SetBloque(DependencyObject destino, BloqueTabla? valor) =>
            destino.SetValue(BloqueProperty, valor);

        public static BloqueTabla? GetBloque(DependencyObject destino) =>
            (BloqueTabla?)destino.GetValue(BloqueProperty);

        private static void AlCambiarElBloque(DependencyObject destino, DependencyPropertyChangedEventArgs e)
        {
            if (destino is not ContentControl contenedor)
                return;

            contenedor.Content = e.NewValue is BloqueTabla bloque ? Construir(contenedor, bloque) : null;
        }

        private static UIElement Construir(FrameworkElement contexto, BloqueTabla bloque)
        {
            var linea = Pincel(contexto, "Linea", Colors.LightGray);
            var tinta = Pincel(contexto, "TintaFuerte", Colors.Black);
            var tenue = Pincel(contexto, "TintaTenue", Colors.Gray);
            var acentoSuave = Pincel(contexto, "AcentoSuave", Colors.WhiteSmoke);

            var grilla = new Grid();

            foreach (var columna in bloque.Columnas)
                grilla.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(columna.Peso, GridUnitType.Star)
                });

            // Una fila por renglón, más la del encabezado.
            for (int i = 0; i <= bloque.Filas.Count; i++)
                grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ---- Encabezado ----
            grilla.Children.Add(Franja(0, bloque.Columnas.Count, Brushes.Transparent, linea, 0, 1));

            for (int c = 0; c < bloque.Columnas.Count; c++)
                grilla.Children.Add(Celda(bloque.Columnas[c].Encabezado.ToUpperInvariant(), 0, c,
                                          bloque.Columnas[c].ALaDerecha, tenue, negrita: true, tamaño: 11.5));

            // ---- Filas ----
            for (int f = 0; f < bloque.Filas.Count; f++)
            {
                var fila = bloque.Filas[f];
                int r = f + 1;

                var fondo = fila.EsTotal ? acentoSuave : Brushes.Transparent;
                grilla.Children.Add(Franja(r, bloque.Columnas.Count, fondo, linea,
                                           arriba: fila.EsTotal ? 1 : 0,
                                           abajo: fila.EsTotal ? 0 : 1));

                for (int c = 0; c < bloque.Columnas.Count; c++)
                {
                    string texto = c < fila.Celdas.Count ? fila.Celdas[c] : string.Empty;

                    grilla.Children.Add(Celda(texto, r, c, bloque.Columnas[c].ALaDerecha,
                                              tinta, negrita: fila.EsTotal, tamaño: 13));
                }
            }

            return grilla;
        }

        /// <summary>El fondo y la línea divisoria de una fila, por debajo de sus celdas.</summary>
        private static UIElement Franja(int fila, int columnas, Brush fondo, Brush linea, int arriba, int abajo)
        {
            var borde = new Border
            {
                Background = fondo,
                BorderBrush = linea,
                BorderThickness = new Thickness(0, arriba, 0, abajo)
            };

            Grid.SetRow(borde, fila);
            Grid.SetColumn(borde, 0);
            Grid.SetColumnSpan(borde, columnas);

            return borde;
        }

        private static UIElement Celda(string texto, int fila, int columna, bool aLaDerecha,
                                       Brush color, bool negrita, double tamaño)
        {
            var bloque = new TextBlock
            {
                Text = texto,
                FontSize = tamaño,
                Foreground = color,
                FontWeight = negrita ? FontWeights.SemiBold : FontWeights.Normal,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = aLaDerecha ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(6, 7, 6, 7)
            };

            Grid.SetRow(bloque, fila);
            Grid.SetColumn(bloque, columna);

            return bloque;
        }

        /// <summary>
        /// Los colores salen del diccionario de estilos y no de literales: si mañana cambia
        /// la paleta del tablero, la vista previa cambia con ella.
        /// </summary>
        private static Brush Pincel(FrameworkElement contexto, string clave, Color reserva) =>
            contexto.TryFindResource(clave) as Brush ?? new SolidColorBrush(reserva);
    }
}
