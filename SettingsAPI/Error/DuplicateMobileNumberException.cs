using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class DuplicateMobileNumberException : ValidationException
    {
        public DuplicateMobileNumberException() : base(AppMessage.MobileAlreadyInUse)
        {
        }
    }
}