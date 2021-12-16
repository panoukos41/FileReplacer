using System;
using System.Globalization;
using System.Windows.Data;

namespace FileReplacer;

public class SmartEnumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return "";

        return ((OperationResult)value).Name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
