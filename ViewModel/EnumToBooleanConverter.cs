using System.Globalization;
using System.Windows.Data;

namespace ViewModel
{
    // Converts enum value to bool by comparing ToString() to ConverterParameter
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || value == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not used; UI uses command to change mode
            return Binding.DoNothing;
        }
    }
}
