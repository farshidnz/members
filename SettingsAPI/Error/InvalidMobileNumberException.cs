using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidMobileNumberException : ValidationException
    {
        public InvalidMobileNumberException() : base(string.Format(AppMessage.FieldInvalid, "Phone"))
        {
        }
    }
}