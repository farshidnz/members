using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidAppVersionException : ValidationException
    {
        public InvalidAppVersionException() : base(string.Format(AppMessage.FieldInvalid,
            "App version"))
        {
        }
    }
}