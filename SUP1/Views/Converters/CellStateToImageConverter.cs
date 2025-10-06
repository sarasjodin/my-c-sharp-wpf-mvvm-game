using SUP.Models.Enums;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SUP.Views.Converters
{
    public class CellStateToImageConverter : IValueConverter
    {
        private static readonly BitmapImage EmptyImg = Load("pack://application:,,,/Assets/Icons/cell.png");
        private static readonly BitmapImage XImg = Load("pack://application:,,,/Assets/Icons/cutlery.png");
        private static readonly BitmapImage OImg = Load("pack://application:,,,/Assets/Icons/pancake.png");

        private static BitmapImage Load(string packUri)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(packUri, UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                CellState.X => XImg,
                CellState.O => OImg,
                CellState.Empty => EmptyImg,
                _=> EmptyImg
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}