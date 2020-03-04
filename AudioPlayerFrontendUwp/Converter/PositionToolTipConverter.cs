using StdOttStandard;
using System;
using Windows.UI.Xaml.Data;

namespace AudioPlayerFrontend
{
    class PositionToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return StdUtils.ToString(TimeSpan.FromSeconds((double)value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
