using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class DuplicatePaypalAccountException : ValidationException
    {
        public DuplicatePaypalAccountException() : base(AppMessage.PaypalAccountDuplicate)
        {
        }
    }
}