using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AudioPlayerFrontendWpf
{
    class FilePathsToSingleStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return string.Empty;

            return string.Join("\r\n", (IEnumerable<string>)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (string)value;

            if (text.Length == 0) return null;

            return text.Replace("\r", "").Split('\n').ToArray();
        }
    }
}
