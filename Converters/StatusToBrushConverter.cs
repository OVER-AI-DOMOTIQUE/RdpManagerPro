using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RDPManager.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOnline)
            {
                return isOnline
                    ? new SolidColorBrush(Color.FromRgb(0x23, 0x86, 0x36)) // Green
                    : new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33)); // Red
            }
            return new SolidColorBrush(Color.FromRgb(0x48, 0x4F, 0x58)); // Gray - unknown
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
                return isExpanded ? 1.0 : 0.6;
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
