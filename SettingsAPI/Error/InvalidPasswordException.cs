using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidPasswordException : ValidationException
    {
        public InvalidPasswordException(string field) : base(string.Format(AppMessage.FieldInvalid, field) +
                                                             ", length must be at least 8 characters")
        {
        }
    }
}