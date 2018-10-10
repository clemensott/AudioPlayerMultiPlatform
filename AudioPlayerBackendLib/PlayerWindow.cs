using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerBackendLib
{
    class PlayerWindow : Window
    {
        public PlayerWindow(MediaElement mediaElement)
        {
            Visibility = Visibility.Hidden;

            Content = mediaElement;
        }
    }
}