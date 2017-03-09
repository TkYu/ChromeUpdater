using System;
using System.Windows.Data;

namespace ChromeUpdater.ArthasUI.Converters
{
    // http://stackoverflow.com/questions/397556/how-to-bind-radiobuttons-to-an-enum
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(true) ?? false ? parameter : Binding.DoNothing;
        }
    }
}
