using AudioPlayerBackend;
using StdOttUwp.Converters;
using Windows.UI.Xaml;

namespace AudioPlayerFrontend
{
    class IsIMqttAudioConverter : IsTypeToValueConverter<IMqttAudio>
    {
        public IsIMqttAudioConverter()
        {
            EqualsValue = Visibility.Visible;
            NotEqualsValue = Visibility.Collapsed;
        }
    }
}
