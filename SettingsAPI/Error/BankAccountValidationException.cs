using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class BankAccountValidationException : ValidationException
    {
        public BankAccountValidationException(string field) : base(string.Format(AppMessage.FieldInvalid,
            field))
        {
        }
    }
}