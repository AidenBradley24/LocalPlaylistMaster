using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LocalPlaylistMaster.Extensions
{
    public class IntStringMapConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty ValueMapProperty =
            DependencyProperty.Register("ValueMap", typeof(Dictionary<int, string>), typeof(IntStringMapConverter), new PropertyMetadata(null));

        public Dictionary<int, string> ValueMap
        {
            get { return (Dictionary<int, string>)GetValue(ValueMapProperty); }
            set 
            { 
                SetValue(ValueMapProperty, value);
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int keyValue && ValueMap != null)
            {
                if (ValueMap.TryGetValue(keyValue, out string? outVal))
                {
                    return outVal;
                }
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
