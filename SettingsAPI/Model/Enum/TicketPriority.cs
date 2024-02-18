using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TicketPriority
    {
        [Description("Low")] [EnumMember] Low = 1,
        [Description("Medium")] [EnumMember] Medium = 2,
        [Description("High")] [EnumMember] High	 = 3,
        [Description("Urgent")] [EnumMember] Urgent = 4,
    }
}