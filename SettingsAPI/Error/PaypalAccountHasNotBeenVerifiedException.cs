using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class PaypalAccountHasNotBeenVerifiedException : ValidationException
    {
        public PaypalAccountHasNotBeenVerifiedException(string message) : base(message)
        {
        }
    }
}