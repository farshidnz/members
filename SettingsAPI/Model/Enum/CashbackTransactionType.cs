using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum CashbackTransactionType
    {
        [Description("None")] [EnumMember]
        None = 0,

        [Description("Promo - Refer a mate (mate)")][EnumMember]
        RafFriend =  1,

        [Description("Promo - Refer a mate (referrer)")] [EnumMember]
        RafReferrer = 2,

        [Description("Promo - $5 reactivation offer")] [EnumMember]
        Promo5DollarReactivationOffer = 3,

        [Description("Promo - $5 signup bonus")] [EnumMember]
        Promo5DollarSignupBonus = 4,

        [Description("Sale")] [EnumMember]
        Sale = 5,

        [Description("Cashback claim")] [EnumMember]
        CashbackClaim = 6,

        [Description("Promotion Sale")] [EnumMember]
        PromotionSale = 7,

        [Description("Account Correction")] [EnumMember]
        AccountCorrection = 8,

        [Description("Promo MS")] [EnumMember]
        PromoMS = 9,

        [Description("Community Bonus")] [EnumMember]
        CommunityBonus = 10,

        [Description("Community Top Up")] [EnumMember]
        CommunityTopUp = 11
    }
}