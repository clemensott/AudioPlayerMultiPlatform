using AudioPlayerBackend;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace AudioPlayerFrontend
{
    class LoopIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((LoopType)value)
            {
                case LoopType.Next:
                    return Symbol.Next;

                case LoopType.Stop:
                    return Symbol.Stop;

                case LoopType.CurrentPlaylist:
                    return Symbol.RepeatAll;

                case LoopType.CurrentSong:
                    return Symbol.RepeatOne;
            }

            throw new ArgumentException("LoopType \"" + value + "\" is not implemented.", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
