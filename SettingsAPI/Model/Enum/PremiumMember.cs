using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum PremiumMember
    {
        [Description("Yes")] [EnumMember] Yes,
        [Description("No")] [EnumMember] No
    }
}