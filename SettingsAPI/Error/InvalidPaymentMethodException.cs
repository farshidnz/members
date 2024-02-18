using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidPaymentMethodException : ValidationException
    {
        public InvalidPaymentMethodException() : base(string.Format(AppMessage.FieldInvalid, "Payment method"))
        {
        }
    }
}