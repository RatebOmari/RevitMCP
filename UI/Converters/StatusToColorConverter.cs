using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RevitClaudeMCP.UI
{
    /// <summary>
    /// Converts server status text to color for status indicator
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "running":
                        return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                    case "stopped":
                        return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
                    case "error":
                        return new SolidColorBrush(Color.FromRgb(230, 126, 34)); // Orange
                    default:
                        return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Gray
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
