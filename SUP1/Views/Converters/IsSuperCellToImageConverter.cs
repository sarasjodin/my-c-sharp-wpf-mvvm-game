using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SUP.Views.Converters;

public class IsSuperCellToImageConverter : IValueConverter
{
    public string SuperCellUri { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSuperCell && isSuperCell)
        {
            return new Uri(SuperCellUri, UriKind.Relative);
        }
        return System.Windows.DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
