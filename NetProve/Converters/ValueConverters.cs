using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NetProve.Models;

namespace NetProve.Converters
{
    /// <summary>Maps a 0-100 float to a status color (green→yellow→red).</summary>
    public sealed class PercentToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float v = System.Convert.ToSingle(value);
            return v >= 90 ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))  // red
                : v >= 70 ? new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B))  // amber
                : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));             // green
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Maps ping value to color.</summary>
    public sealed class PingToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = System.Convert.ToDouble(value);
            return v >= 150 ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
                : v >= 80 ? new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B))
                : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Maps network quality enum to a color.</summary>
    public sealed class NetworkQualityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return s switch
                {
                    "Excellent" => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                    "Good" => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                    "Fair" => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                    _ => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Maps severity to color.</summary>
    public sealed class SeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return s switch
                {
                    "None" => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                    "Low" => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                    "Medium" => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                    "High" => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                    "Critical" => new SolidColorBrush(Color.FromRgb(0x7F, 0x1D, 0x1D)),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Bool → Visibility.</summary>
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
            v is Visibility vis && vis == Visibility.Visible;
    }

    /// <summary>Formats bytes to human-readable string.</summary>
    public sealed class BytesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long b = System.Convert.ToInt64(value);
            if (b >= 1_073_741_824) return $"{b / 1_073_741_824f:F1} GB";
            if (b >= 1_048_576) return $"{b / 1_048_576f:F0} MB";
            if (b >= 1024) return $"{b / 1024f:F0} KB";
            return $"{b} B";
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Cache usage percentage to color.</summary>
    public sealed class CacheUsageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float pct = System.Convert.ToSingle(value);
            return pct >= 100 ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
                : pct >= 70 ? new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B))
                : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Bool → HorizontalAlignment (for chat bubbles).</summary>
    public sealed class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Bool → Chat bubble background color.</summary>
    public sealed class BoolToChatBubbleBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value
                ? new SolidColorBrush(Color.FromArgb(0xCC, 0x3B, 0x82, 0xF6))  // user: blue
                : new SolidColorBrush(Color.FromArgb(0xCC, 0x33, 0x40, 0x55)); // bot: dark gray
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Bool → Active/Inactive text.</summary>
    public sealed class BoolToActiveConverter : IValueConverter
    {
        public string TrueText { get; set; } = "Active";
        public string FalseText { get; set; } = "Inactive";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? TrueText : FalseText;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    /// <summary>Converts ProcessPriorityClass enum to localized string.</summary>
    public sealed class PriorityToStringConverter : IValueConverter
    {
        private readonly Localization.LocalizationManager _loc = Localization.LocalizationManager.Instance;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Diagnostics.ProcessPriorityClass p)
                return p switch
                {
                    System.Diagnostics.ProcessPriorityClass.Normal => _loc["PriorityNormal"],
                    System.Diagnostics.ProcessPriorityClass.BelowNormal => _loc["PriorityBelowNormal"],
                    System.Diagnostics.ProcessPriorityClass.AboveNormal => _loc["PriorityAboveNormal"],
                    System.Diagnostics.ProcessPriorityClass.High => _loc["PriorityHigh"],
                    System.Diagnostics.ProcessPriorityClass.Idle => _loc["PriorityIdle"],
                    System.Diagnostics.ProcessPriorityClass.RealTime => _loc["PriorityRealTime"],
                    _ => p.ToString()
                };
            return value?.ToString() ?? "";
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }
}
