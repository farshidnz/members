using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TopicEnquiry
    {
        [Description("Cashback issues")] [EnumMember] CashbackIssues,
        [Description("Payment issues")] [EnumMember] PaymentIssues,
        [Description("Refer a friend")] [EnumMember] ReferAFriend
    }
}