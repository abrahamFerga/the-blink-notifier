// BlinkNotifier.App — helper converter for validation error visibility
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlinkNotifier.App.Settings;

[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
