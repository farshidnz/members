using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TierTypeEnum
    {
        [Description("Discount")]
        [EnumMember]
        Discount = 117,

        [Description("MaxDiscount")]
        [EnumMember]
        MaxDiscount = 121
    }
}