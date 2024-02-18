using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TransactionOrderByField
    {
        [Description("Date")] [EnumMember] Date = 0,
        [Description("Name")] [EnumMember] Name = 1,
        [Description("Amount")] [EnumMember] Amount = 2,
        [Description("Status")] [EnumMember] Status = 3
    }
}