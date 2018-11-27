using AudioPlayerBackend;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioPlayerFrontendWpf
{
    class ToIAudioClientConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as IMqttAudioClient;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
