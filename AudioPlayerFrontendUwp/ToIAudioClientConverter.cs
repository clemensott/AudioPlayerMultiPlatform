using AudioPlayerBackend;
using System;
using Windows.UI.Xaml.Data;

namespace AudioPlayerFrontend
{
    class ToIAudioClientConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value as IMqttAudioClient;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
