using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum BankAccountValidationFields
    {
        [Description("Account number")] [EnumMember]
        AccountNumber = 0,
        [Description("Bsb")] [EnumMember] Bsb = 1,

        [Description("Account name")] [EnumMember]
        AccountName = 2
    }
}