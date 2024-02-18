using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class MemberBankAccountNotFoundException : ValidationException
    {
        public MemberBankAccountNotFoundException() : base(string.Format(AppMessage.ObjectNotFound,
            "Member bank account"))
        {
        }
    }
}