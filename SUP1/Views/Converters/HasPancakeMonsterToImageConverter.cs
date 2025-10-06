using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SUP.Views.Converters;

public class HasPancakeMonsterToImageConverter : IValueConverter
{
    public string PancakeMonsterUri { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasPancakeMonster && hasPancakeMonster)
        {
            return new Uri(PancakeMonsterUri, UriKind.Relative);
        };

        return System.Windows.DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

