using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TransactionType
    {
        [Description("Cashback")] [EnumMember]
        Cashback = 0,

        [Description("Savings")] [EnumMember]
        Savings = 1,

        [Description("Withdrawal")] [EnumMember]
        Withdrawal = 2,
    }
}