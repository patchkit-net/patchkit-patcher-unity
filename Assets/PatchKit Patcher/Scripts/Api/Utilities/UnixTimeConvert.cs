using System;

namespace PatchKit.Api.Utilities
{
    /// <summary>
    /// Utility for converting Unix time stamp.
    /// </summary>
    public static class UnixTimeConvert
    {
        /// <summary>
        /// Converts Unix time stamp to DateTime value.
        /// </summary>
        public static DateTime FromUnixTimeStamp(double unixTimeStamp)
        {
            var baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            baseDateTime = baseDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return baseDateTime;
        }

        /// <summary>
        /// Converts DateTime value to Unix time stamp.
        /// </summary>
        public static double ToUnixTimeStamp(DateTime dateTime)
        {
            var baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var dateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(dateTime);

            return (dateTimeUtc - baseDateTime).TotalSeconds;
        }
    }
}
