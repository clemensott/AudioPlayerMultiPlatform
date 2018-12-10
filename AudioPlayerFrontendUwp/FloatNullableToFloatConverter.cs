using System;
using Windows.UI.Xaml.Data;

namespace AudioPlayerFrontend
{
    class FloatNullableToFloatConverter : IValueConverter
    {
        private float destValue;
        private float? srcValue;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            srcValue = (float?)value;
            return destValue = srcValue ?? destValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            float newValue = (float)value;

            if (newValue == destValue) return srcValue;

            return destValue = newValue;
        }
    }
}
