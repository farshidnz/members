using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum BonusIssue
    {
        [Description("Overdue pending bonus")] [EnumMember] OverduePendingBonus,
        [Description("Declined bonus")] [EnumMember] DeclinedBonus
    }
}