using System;
using System.Runtime.InteropServices;
using SettingsAPI.Common;

namespace SettingsAPI.Service
{
    public class TimeService : ITimeService
    {
        public long GetCurrentTimestamp()
        {
            return Util.GetCurrentTimestamp();
        }

        public DateTime GetCurrentDateTimeToday()
        {
            return DateTime.Today;
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime Now
        {
            get
            {
                TimeZoneInfo cstZone = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                                TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney") : TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");

                return TimeZoneInfo.ConvertTimeFromUtc(UtcNow, cstZone);
            }
        }
    }
}