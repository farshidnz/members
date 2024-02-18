using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class TransactionNotFoundException : ValidationException
    {
        public TransactionNotFoundException() : base(string.Format(AppMessage.ObjectNotFound, "Transaction"))
        {
        }
    }
}