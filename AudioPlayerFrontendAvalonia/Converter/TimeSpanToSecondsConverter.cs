using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AudioPlayerFrontendAvalonia.Converter;

public class TimeSpanToSecondsConverter : IValueConverter
{
    private static TimeSpanToSecondsConverter instance;
    public static TimeSpanToSecondsConverter Instance => instance ??= new TimeSpanToSecondsConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TimeSpan timeSpan ? timeSpan.TotalSeconds : 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is double seconds ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
    }
}