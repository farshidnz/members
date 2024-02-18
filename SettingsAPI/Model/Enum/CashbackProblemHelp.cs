using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum CashbackProblemHelp
    {
        [Description("Cashback claim")] [EnumMember]
        CashbackClaim,

        [Description("MoneyMe Perks feedback")] [EnumMember]
        MoneyMePerksFeedback,
        [Description("Perks rewards claim")] PerksRewardsClaim,

        [Description("Perks rewards issues")] [EnumMember]
        PerksRewardsIssues,

        [Description("My Rewards support")] [EnumMember]
        MyRewardsSupport,

        [Description("General Support and Site Feedback")] [EnumMember]
        GeneralSupportAndSiteFeedback,

        [Description("General Enquiry")] [EnumMember]
        GeneralEnquiry,
        [Description("Other")] [EnumMember] Other
    }
}