using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AudioPlayerFrontend
{
    class IsAudioStreamPlayerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Visibility.Collapsed;
            //return value is AudioStreamPlayer ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
