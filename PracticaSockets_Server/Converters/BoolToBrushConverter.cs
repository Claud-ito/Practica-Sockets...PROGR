using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PracticaSockets_Server.Converters
{
    public class BoolToBrushConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    => new SolidColorBrush(value is true
        ? Color.FromRgb(166, 227, 161)   // verde
        : Color.FromRgb(88, 91, 112));  // gris

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
