using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidOperatingSystemException : ValidationException
    {
        public InvalidOperatingSystemException() : base(string.Format(AppMessage.FieldInvalid,
            "Operating system"))
        {
        }
    }
}