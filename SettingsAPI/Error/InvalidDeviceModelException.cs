using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidDeviceModelException : ValidationException
    {
        public InvalidDeviceModelException() : base(string.Format(AppMessage.FieldInvalid,
            "Device model"))
        {
        }
    }
}