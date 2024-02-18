using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidEmailVerificationCodeException : ValidationException
    {
        public InvalidEmailVerificationCodeException() : base(string.Format(AppMessage.FieldInvalid,
            "Email verification code"))
        {
        }
    }
}