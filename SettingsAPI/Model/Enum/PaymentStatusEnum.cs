using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum PaymentStatusEnum
    {
        [Description("Paid")] [EnumMember] Paid = 100,
        [Description("Pending")] [EnumMember] Pending = 101,
        [Description("Declined")] [EnumMember] Declined = 102
    }
}