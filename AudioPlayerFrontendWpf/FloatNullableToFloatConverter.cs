using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioPlayerFrontendWpf
{
    class FloatNullableToFloatConverter : IValueConverter
    {
        private float destValue;
        private float? srcValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            srcValue = (float?)value;
            return destValue = srcValue ?? destValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float newValue = (float)value;

            if (newValue == destValue) return srcValue;

            return destValue = newValue;
        }
    }
}
