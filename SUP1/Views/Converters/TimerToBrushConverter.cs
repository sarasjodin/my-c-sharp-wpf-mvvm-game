using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SUP.Views.Converters
{
    public class TimerToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush White = new(Colors.White);
        private static readonly SolidColorBrush Red = new(Colors.DarkRed);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Red : White;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
