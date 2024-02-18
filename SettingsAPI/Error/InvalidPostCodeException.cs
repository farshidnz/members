using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidPostCodeException : ValidationException
    {
        public InvalidPostCodeException() : base(string.Format(AppMessage.FieldInvalid, "Post code"))
        {
        }
    }
}