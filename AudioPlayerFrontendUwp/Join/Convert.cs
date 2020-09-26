using System;

namespace AudioPlayerFrontend.Join
{
    static class Convert
    {
        private static T ToEnum<T>(Enum value)
        {
            Type type = typeof(T);
            string name = Enum.GetName(value.GetType(), value);

            return (T)Enum.Parse(type, name);
        }
    }
}
