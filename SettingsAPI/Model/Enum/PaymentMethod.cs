using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum PaymentMethod
    {
        [Description("Bank")] [EnumMember] Bank = 1,
        [Description("PayPal")] [EnumMember] PayPal = 2
    }
}