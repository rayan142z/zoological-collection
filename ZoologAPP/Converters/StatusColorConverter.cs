using System.Globalization;

namespace ZoologAPP.Converters;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string) switch
        {
            "verfügbar"  => Color.FromArgb("#10B981"),
            "ausgeliehen" => Color.FromArgb("#F59E0B"),
            "verloren"   => Color.FromArgb("#EF4444"),
            "zerstört"   => Color.FromArgb("#6B7280"),
            _            => Color.FromArgb("#6B7280")
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
