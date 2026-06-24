using System.Globalization;
using System.Windows.Data;
using StickItApp.Services;

namespace StickItApp.Converters;

public sealed class LocalizedDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DisplayTextService.ToDisplayText(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
