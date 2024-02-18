using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidInstallNotifierException : ValidationException
    {
        public InvalidInstallNotifierException() : base(string.Format(AppMessage.FieldInvalid,
            "Install notifier status"))
        {
        }
    }
}