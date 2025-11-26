using System;
using System.Globalization;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using Avalonia.Data.Converters;

namespace AudioPlayerFrontendAvalonia.Converter;

public class HasPlaybackProgressConverter: IValueConverter
{
    private static HasPlaybackProgressConverter instance;
    public static HasPlaybackProgressConverter Instance = instance ??= new HasPlaybackProgressConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is SongRequest songRequest && songRequest.Duration > TimeSpan.Zero;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}