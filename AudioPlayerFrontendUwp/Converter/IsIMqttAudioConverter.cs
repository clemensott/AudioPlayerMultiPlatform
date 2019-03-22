using StdOttUwp.Converters;
using Windows.UI.Xaml;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend
{
    class IsIMqttAudioConverter : IsTypeToValueConverter<AudioStreamPlayer>
    {
        public IsIMqttAudioConverter()
        {
            EqualsValue = Visibility.Visible;
            NotEqualsValue = Visibility.Collapsed;
        }
    }
}
