using System;
using System.Globalization;
using System.Windows.Data;

namespace StoreG5G11.views;

public class EnumBindingSourceConverter : IValueConverter
{
    public static EnumBindingSourceConverter Instance { get; } = new EnumBindingSourceConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Type enumType && enumType.IsEnum)
        {
            return Enum.GetValues(enumType);
        }
        return Array.Empty<object>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}