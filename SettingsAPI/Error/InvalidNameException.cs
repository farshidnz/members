using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidNameException : ValidationException
    {
        public InvalidNameException(string field) : base(string.Format(AppMessage.FieldInvalid, field))
        {
        }
    }
}