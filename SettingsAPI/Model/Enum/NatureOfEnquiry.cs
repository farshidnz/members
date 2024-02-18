using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum NatureOfEnquiry
    {
        [Description("Overdue pending rewards")] [EnumMember]
        OverduePendingRewards,

        [Description("Incorrect Cashback")] [EnumMember]
        IncorrectCashback,

        [Description("Cashback/Rewards declined")] [EnumMember]
        CashbackRewardsDeclined,

        [Description("Incorrect Perks rewards")] [EnumMember]
        IncorrectPerksRewards,

        [Description("Perks rewards declined")] [EnumMember]
        PerksRewardsDeclined
    }
}