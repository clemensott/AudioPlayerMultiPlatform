using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AudioPlayerFrontendAvalonia.Converter;

public delegate object? ConvertEventHandler(
    object? value,
    Type targetType,
    object? parameter,
    CultureInfo culture);

public delegate object? ConvertBackEventHandler(
    object? value,
    Type targetType,
    object? parameter,
    CultureInfo culture);

public class ValueConverter : IValueConverter
{
    public event ConvertEventHandler? ConvertEvent;

    public event ConvertBackEventHandler? ConvertBackEvent;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ConvertEventHandler? convertEvent = this.ConvertEvent;
        return convertEvent != null ? convertEvent(value, targetType, parameter, culture) : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ConvertBackEventHandler? convertBackEvent = this.ConvertBackEvent;
        return convertBackEvent != null ? convertBackEvent(value, targetType, parameter, culture) : null;
    }
}