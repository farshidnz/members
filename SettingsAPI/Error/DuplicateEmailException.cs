using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class DuplicateEmailException : ValidationException
    {
        public DuplicateEmailException() : base(AppMessage.EmailAlreadyInUse)
        {
        }
    }
}