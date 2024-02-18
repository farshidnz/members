using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidMobileOtpException : ValidationException
    {
        public InvalidMobileOtpException() : base(AppMessage.MobileOtpIncorrect)
        {
        }
    }
}