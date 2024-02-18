using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class PaypalAuthorizationCodeUnauthorizedException : ValidationException
    {
        public PaypalAuthorizationCodeUnauthorizedException(string message) : base(message)
        {
        }
    }
}