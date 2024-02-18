using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class DuplicateBankAccountException : ValidationException
    {
        public DuplicateBankAccountException() : base(AppMessage.BankAccountDuplicate)
        {
        }
    }
}