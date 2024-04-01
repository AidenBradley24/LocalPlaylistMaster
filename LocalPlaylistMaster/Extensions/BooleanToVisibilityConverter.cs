using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LocalPlaylistMaster.Extensions
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value is not bool)
                return Visibility.Collapsed;      
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value is not Visibility)
                return false;
            return ((Visibility)value == Visibility.Visible);
        }
    }
}
