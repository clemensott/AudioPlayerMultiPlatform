using System;
using Windows.UI.Xaml.Data;

namespace AudioPlayerFrontend
{
    class VolumeToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.Format("{0}%", (int)((double)value * 100));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
