using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    /**
     *This enum defined description which receives from front end
     * 
     */
    public enum EnquiryReason
    {
        [Description("Incorrect cashback")] [EnumMember]
        IncorrectCashback,

        [Description("Overdue cashback")] [EnumMember]
        OverdueCashback,

        [Description("Declined cashback")] [EnumMember]
        DeclinedCashback,

        [Description("Cashback claim")] [EnumMember]
        CashbackClaim,

        [Description("No cashback bonus")] [EnumMember]
        NoCashbackBonus,

        [Description("Overdue Withdrawal")] [EnumMember]
        OverdueWithdrawal,

        [Description("Overdue referral bonus")] [EnumMember]
        OverdueReferralBonus,

        [Description("Declined referral bonus")] [EnumMember]
        DeclinedReferralBonus,

        [Description("Other")] [EnumMember] Other
    }

    /**
     *This enum defined description which config on freshdesk
     * 
     */
    public enum EnquiryReasonConfiguration
    {
        [Description("My cashback did not track")] [EnumMember]
        CashbackDidNotTrack,

        [Description("Other")] [EnumMember] Other
    }
}