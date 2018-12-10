using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace AudioPlayerFrontend
{
    class FilePathsToSingleStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null) return string.Empty;

            return string.Join("\r\n", (IEnumerable<string>)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string text = (string)value;

            if (text.Length == 0) return null;

            return text.Replace("\r", "").Split('\n').ToArray();
        }
    }
}
