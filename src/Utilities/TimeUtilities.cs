using System;

namespace MKVHelper.Utilities {
    public class TimeUtilities {
        public static double ConvertToSeconds(string timeString) {
            if (TimeSpan.TryParse(timeString, out TimeSpan timeSpan)) {
                return timeSpan.TotalSeconds;
            }
            else {
                throw new FormatException("Invalid time format");
            }
        }

        public static string TrimTrailingZeros(string timeString) {
            return timeString.TrimEnd('0').TrimEnd('.');
        }
    }
}
