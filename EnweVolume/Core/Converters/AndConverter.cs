using System.Globalization;
using System.Windows.Data;

namespace EnweVolume.Core.Converters;

public class AndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values != null && values.All(v => v is bool b && b);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
