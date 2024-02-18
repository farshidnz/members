using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class MemberNoAvailableBalanceException : ValidationException
    {
        public MemberNoAvailableBalanceException() : base(AppMessage.MemberNoAvailableBalance)
        {
        }
    }
}