using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidUriException : ValidationException
    {
        public InvalidUriException() : base(string.Format(AppMessage.FieldInvalid, "Uri"))
        {
        }
    }
}