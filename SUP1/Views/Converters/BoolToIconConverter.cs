using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SUP.Views.Converters;

public class BoolToIconConverter : IValueConverter
{
    public string OnUri { get; set; }
    public string OffUri { get; set; }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool isMuted) return Binding.DoNothing;

        if (isMuted)
        {
            return new Uri(OffUri, uriKind: UriKind.Relative);
        }
        else
        {
            return new Uri(OnUri, uriKind: UriKind.Relative);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
