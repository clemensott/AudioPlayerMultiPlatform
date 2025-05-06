using System;
using System.Globalization;
using Avalonia.Data.Converters;
using StdOttStandard;

namespace AudioPlayerFrontendAvalonia.Converter;

public class TimeSpanToStringConverter : IValueConverter
{
    private static TimeSpanToStringConverter instance;
    public static TimeSpanToStringConverter Instance = instance ??= new TimeSpanToStringConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TimeSpan timeSpan ? StdUtils.ToString(timeSpan, false) : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text ? TimeSpan.Parse(text) : TimeSpan.Zero;
    }
}