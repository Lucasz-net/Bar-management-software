using System;
using System.Globalization;
using System.Windows.Data;

namespace SistemaGestionBar.Converters
{
    public class AnchoAColumnasConverter : IValueConverter
    {
        private const double AnchoDeReferencia = 180;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double ancho = value is double d ? d : 0;
            int columnas = (int)(ancho / AnchoDeReferencia);
            return Math.Max(columnas, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
