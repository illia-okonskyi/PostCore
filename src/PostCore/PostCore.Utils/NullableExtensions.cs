using System;

namespace PostCore.Utils
{
    public static class NullableExtensions
    {
        public static bool TryParse(string value, out long? result)
        {
            result = long.TryParse(value, out long tempResult) ? tempResult : default(long?);
            return (result == null) ? false : true;
        }

        public static bool TryParse(string value, out DateTime? result)
        {
            result = DateTime.TryParse(value, out DateTime tempResult) ? tempResult : default(DateTime?);
            return (result == null) ? false : true;
        }

        public static bool TryParse<T>(string value, out T? result) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("Invalid Enum");
            result = Enum.TryParse(value, out T tempResult) ? tempResult : default(T?);
            return (result == null) ? false : true;
        }
    }
}
