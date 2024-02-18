using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum PaymentIssuesEnum
    {
        [Description("Withdrawn payment has not been paid into nominated bank account")] [EnumMember] PaymentHasNotBeenPaid,
        [Description("Other")] [EnumMember] Other,
    }
}