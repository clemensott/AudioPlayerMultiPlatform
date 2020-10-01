using AudioPlayerBackend.Audio;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace AudioPlayerFrontend
{
    class OrderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((OrderType)value)
            {
                case OrderType.ByTitleAndArtist:
                    return Symbol.ShowResults;

                case OrderType.ByPath:
                    return Symbol.ShowBcc;

                case OrderType.Custom:
                    return Symbol.Shuffle;
            }

            throw new ArgumentException("OrderType \"" + value + "\" is not implemented.", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
