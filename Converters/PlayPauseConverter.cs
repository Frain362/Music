using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class PlayPauseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying && isPlaying)
                return "⏸";
            return "▶️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}