using System;
using System.Globalization;
using AudioPlayerBackend.Player;
using Avalonia.Data.Converters;

namespace AudioPlayerFrontendAvalonia.Converter;

public class IsPlayingConverter : IValueConverter
{
    private static IsPlayingConverter instance;
    public static IsPlayingConverter Instance = instance ??= new IsPlayingConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is PlaybackState.Playing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? PlaybackState.Playing : PlaybackState.Paused;
    }
}