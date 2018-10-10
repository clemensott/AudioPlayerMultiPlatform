using AudioPlayerBackendLib;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioPlayerFrontendWpf
{
    class IsCheckedToPlayState : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (PlayState)value == PlayState.Play;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? PlayState.Play : PlayState.Pause;
        }
    }
}
