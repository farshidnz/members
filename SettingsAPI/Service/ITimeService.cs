using System;

namespace SettingsAPI.Service
{
    public interface ITimeService
    {
        long GetCurrentTimestamp();
        DateTime GetCurrentDateTimeToday();
        DateTime UtcNow { get; }

        DateTime Now { get; }
    }
}