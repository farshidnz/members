using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum SignupAutomatedVerificationEmailStatus
    {
        [Description("NotSent")] [EnumMember] NotSent = 0,
        [Description("Sending")] [EnumMember] Sending = 1,
        [Description("Sent")] [EnumMember] Sent = 2
    }
}