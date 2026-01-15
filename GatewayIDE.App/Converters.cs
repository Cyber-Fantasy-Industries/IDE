// Converters.cs
using System;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia.Media;
namespace GatewayIDE.App
{
    public sealed class HalfConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d && !double.IsNaN(d) && !double.IsInfinity(d))
                return Math.Max(48, d * 0.5); // mind. 48px, sonst Hälfte
            return 200d;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }







// -----------------------------
// bool -> Brush
// -----------------------------
public sealed class BooleanToBrushConverter : IValueConverter
{
    public static BooleanToBrushConverter Instance { get; } = new();

    // Border
    public IBrush TrueBorder { get; set; } = Brushes.LimeGreen;
    public IBrush FalseBorder { get; set; } = Brushes.IndianRed;
    public IBrush NullBorder { get; set; } = Brushes.Gray;

    // Background (leichte Tints)
    public IBrush TrueBackground { get; set; } = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0xFF, 0x7A));
    public IBrush FalseBackground { get; set; } = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0x3B, 0x30));
    public IBrush NullBackground { get; set; } = new SolidColorBrush(Color.FromArgb(0x22, 0xAA, 0xAA, 0xAA));

    // Foreground
    public IBrush TrueForeground { get; set; } = Brushes.White;
    public IBrush FalseForeground { get; set; } = Brushes.White;
    public IBrush NullForeground { get; set; } = Brushes.LightGray;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool? b = value switch
        {
            null => null,
            bool bb => bb,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => null
        };

        var mode = (parameter?.ToString() ?? "Border").Trim();

        return mode switch
        {
            "Border" => b == true ? TrueBorder : b == false ? FalseBorder : NullBorder,
            "Background" => b == true ? TrueBackground : b == false ? FalseBackground : NullBackground,
            "Foreground" => b == true ? TrueForeground : b == false ? FalseForeground : NullForeground,
            _ => b == true ? TrueBorder : b == false ? FalseBorder : NullBorder
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}


// -----------------------------
// bool -> Text
// -----------------------------
public sealed class BooleanToTextConverter : IValueConverter
{
    public static BooleanToTextConverter Instance { get; } = new();

    public string TrueText { get; set; } = "Online";
    public string FalseText { get; set; } = "Offline";
    public string NullText { get; set; } = "Unknown";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return NullText;
        if (value is bool b) return b ? TrueText : FalseText;

        if (value is string s && bool.TryParse(s, out var parsed))
            return parsed ? TrueText : FalseText;

        return NullText;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// -----------------------------
// 0 -> false, >0 -> true
// (für Counts, z.B. 0 Peers = false)
// -----------------------------
public sealed class ZeroToBoolConverter : IValueConverter
{
    public static ZeroToBoolConverter Instance { get; } = new();

    // optional: invertierbar per ConverterParameter="invert"
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var invert = string.Equals(parameter?.ToString(), "invert", StringComparison.OrdinalIgnoreCase);

        bool result = value switch
        {
            null => false,
            int i => i != 0,
            long l => l != 0,
            double d => Math.Abs(d) > double.Epsilon,
            float f => Math.Abs(f) > float.Epsilon,
            string s when int.TryParse(s, out var i) => i != 0,
            _ => true
        };

        return invert ? !result : result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}



}