using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TicketStatus
    {
        [Description("Open")] [EnumMember] Open = 2,
        [Description("Pending")] [EnumMember] Pending = 3,
        [Description("Resolved")] [EnumMember] Resolved	 = 4,
        [Description("Closed")] [EnumMember] Closed = 5,
    }
}