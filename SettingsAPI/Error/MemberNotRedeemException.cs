using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class MemberNotRedeemException : ValidationException
    {
        public MemberNotRedeemException() : base(AppMessage.MemberNotRedeem)
        {
        }
    }
}