﻿using AudioPlayerBackend.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioPlayerFrontendWpf
{
    class PlayStateToIsCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (PlaybackState)value == PlaybackState.Playing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? PlaybackState.Playing : PlaybackState.Paused;
        }
    }
}
