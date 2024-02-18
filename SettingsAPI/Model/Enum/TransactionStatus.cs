using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TransactionStatus
    {
        [Description("Pending")] [EnumMember] Pending = 100,
        [Description("Confirmed")] [EnumMember] Confirmed = 101,
        [Description("Declined")] [EnumMember] Declined = 102,
        [Description("Approved")] [EnumMember] Approved = 103,
    }
}