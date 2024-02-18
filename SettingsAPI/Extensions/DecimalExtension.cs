using System;

namespace SettingsAPI.Extensions
{
    public static class DecimalExtension
    {
        public static decimal RoundToTwoDecimalPlaces(this decimal d)
        {
            return Math.Round(d, 2);
        }
    }
}