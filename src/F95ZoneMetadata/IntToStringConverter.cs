using System;
using System.Globalization;
using System.Windows.Data;

namespace F95ZoneMetadata;

public class IntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(string)) return "-1";
        return value is not int number ? "-1" : number.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(int)) return -1;
        if (value is not string s) return -1;
        if (int.TryParse(s, out var result)) return result;
        return -1;
    }
}
