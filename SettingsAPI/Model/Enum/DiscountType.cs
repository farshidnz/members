using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum DiscountType
    {
        [Description("Sitewide sale")] [EnumMember] SiteWideSale,
        [Description("Coupon code")] [EnumMember] CouponOrVoucher,
        [Description("Loyalty discount")] LoyaltyDiscount,
        [Description("Others")] [EnumMember] Other,
    }
}