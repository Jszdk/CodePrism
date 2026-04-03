using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ILSpyGUI.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colors)
        {
            var parts = colors.Split('|');
            var color = boolValue && parts.Length > 0 ? parts[0] : (parts.Length > 1 ? parts[1] : "#FFFFFF");
            return new SolidColorBrush(Color.Parse(color));
        }
        return new SolidColorBrush(Color.Parse("#FFFFFF"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string texts)
        {
            var parts = texts.Split('|');
            return boolValue && parts.Length > 0 ? parts[0] : (parts.Length > 1 ? parts[1] : boolValue.ToString());
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
