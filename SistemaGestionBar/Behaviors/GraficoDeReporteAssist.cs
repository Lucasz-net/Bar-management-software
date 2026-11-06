using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SistemaGestionBar.Reportes;

namespace SistemaGestionBar.Behaviors
{
    public static class GraficoDeReporteAssist
    {
        public static readonly DependencyProperty BloqueProperty =
            DependencyProperty.RegisterAttached(
                "Bloque",
                typeof(BloqueGrafico),
                typeof(GraficoDeReporteAssist),
                new PropertyMetadata(null, AlCambiarElBloque));

        public static void SetBloque(DependencyObject destino, BloqueGrafico? valor) =>
            destino.SetValue(BloqueProperty, valor);

        public static BloqueGrafico? GetBloque(DependencyObject destino) =>
            (BloqueGrafico?)destino.GetValue(BloqueProperty);

        private static void AlCambiarElBloque(DependencyObject destino, DependencyPropertyChangedEventArgs e)
        {
            if (destino is not ContentControl contenedor)
                return;

            contenedor.Content = e.NewValue is BloqueGrafico bloque ? Construir(contenedor, bloque) : null;
        }

        private static UIElement Construir(FrameworkElement contexto, BloqueGrafico bloque) =>
            bloque.Tipo == TipoDeGrafico.Circular
                ? ConstruirCircular(contexto, bloque)
                : ConstruirBarras(contexto, bloque);

        private static UIElement ConstruirBarras(FrameworkElement contexto, BloqueGrafico bloque)
        {
            var columna = new StackPanel();
            decimal maximo = bloque.Series.Count == 0 ? 0 : bloque.Series.Max(s => s.Valor);

            for (int i = 0; i < bloque.Series.Count; i++)
            {
                var serie = bloque.Series[i];
                float proporcion = maximo <= 0 ? 0f : (float)(serie.Valor / maximo);

                var fila = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var etiqueta = new TextBlock
                {
                    Text = serie.Etiqueta,
                    FontSize = 12.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Pincel(contexto, "TintaFuerte", Colors.Black)
                };
                Grid.SetColumn(etiqueta, 0);

                var pista = new Grid { Height = 16, Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
                pista.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(proporcion, 0.001f), GridUnitType.Star) });
                pista.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(1f - proporcion, 0.001f), GridUnitType.Star) });

                var fondoPista = new Border
                {
                    Background = Pincel(contexto, "Fondo", Colors.WhiteSmoke),
                    CornerRadius = new CornerRadius(4)
                };
                Grid.SetColumnSpan(fondoPista, 2);
                pista.Children.Add(fondoPista);

                var relleno = new Border
                {
                    Background = new SolidColorBrush(ColorDesdeHex(PaletaDeGrafico.Color(i))),
                    CornerRadius = new CornerRadius(4)
                };
                Grid.SetColumn(relleno, 0);
                pista.Children.Add(relleno);

                Grid.SetColumn(pista, 1);

                var valor = new TextBlock
                {
                    Text = serie.ValorTexto,
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Pincel(contexto, "Acento", Colors.SteelBlue)
                };
                Grid.SetColumn(valor, 2);

                fila.Children.Add(etiqueta);
                fila.Children.Add(pista);
                fila.Children.Add(valor);
                columna.Children.Add(fila);
            }

            return columna;
        }

        private static UIElement ConstruirCircular(FrameworkElement contexto, BloqueGrafico bloque)
        {
            const double radio = 68;

            decimal total = bloque.Series.Sum(s => s.Valor);
            var centro = new Point(radio, radio);

            var grilla = new Grid();
            grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lienzo = new Canvas { Width = radio * 2, Height = radio * 2, Margin = new Thickness(0, 0, 24, 0) };

            if (total <= 0 || bloque.Series.Count == 0)
            {
                lienzo.Children.Add(new Ellipse
                {
                    Width = radio * 2,
                    Height = radio * 2,
                    Fill = Pincel(contexto, "Linea", Colors.LightGray)
                });
            }
            else
            {
                double angulo = -90; // arranca arriba, como las agujas del reloj

                for (int i = 0; i < bloque.Series.Count; i++)
                {
                    double proporcion = (double)(bloque.Series[i].Valor / total);
                    var pincel = new SolidColorBrush(ColorDesdeHex(PaletaDeGrafico.Color(i)));

                    if (bloque.Series.Count == 1 || proporcion >= 0.9995)
                    {
                        lienzo.Children.Add(new Ellipse { Width = radio * 2, Height = radio * 2, Fill = pincel });
                        break;
                    }

                    double siguiente = angulo + proporcion * 360.0;
                    lienzo.Children.Add(Porcion(centro, radio, angulo, siguiente, pincel));
                    angulo = siguiente;
                }
            }

            Grid.SetColumn(lienzo, 0);
            grilla.Children.Add(lienzo);

            var leyenda = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            for (int i = 0; i < bloque.Series.Count; i++)
            {
                var serie = bloque.Series[i];
                string porcentaje = total <= 0 ? string.Empty : $" ({(double)(serie.Valor / total):P0})";

                var renglon = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
                renglon.Children.Add(new Border
                {
                    Width = 12,
                    Height = 12,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(ColorDesdeHex(PaletaDeGrafico.Color(i))),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                renglon.Children.Add(new TextBlock
                {
                    Text = $"{serie.Etiqueta}{porcentaje} · {serie.ValorTexto}",
                    FontSize = 12.5,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Pincel(contexto, "TintaFuerte", Colors.Black)
                });

                leyenda.Children.Add(renglon);
            }

            Grid.SetColumn(leyenda, 1);
            grilla.Children.Add(leyenda);

            return grilla;
        }

        private static Path Porcion(Point centro, double radio, double anguloInicio, double anguloFin, Brush relleno)
        {
            var figura = new PathFigure { StartPoint = centro, IsClosed = true };
            figura.Segments.Add(new LineSegment(PuntoEnCirculo(centro, radio, anguloInicio), isStroked: true));
            figura.Segments.Add(new ArcSegment
            {
                Point = PuntoEnCirculo(centro, radio, anguloFin),
                Size = new Size(radio, radio),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = (anguloFin - anguloInicio) > 180
            });

            var geometria = new PathGeometry();
            geometria.Figures.Add(figura);

            return new Path { Data = geometria, Fill = relleno };
        }

        private static Point PuntoEnCirculo(Point centro, double radio, double grados)
        {
            double radianes = grados * Math.PI / 180.0;
            return new Point(centro.X + radio * Math.Cos(radianes), centro.Y + radio * Math.Sin(radianes));
        }

        private static Brush Pincel(FrameworkElement contexto, string clave, Color reserva) =>
            contexto.TryFindResource(clave) as Brush ?? new SolidColorBrush(reserva);

        private static Color ColorDesdeHex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    }
}
