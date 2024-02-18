using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TicketType
    {
        [Description("User - Question")] [EnumMember] UserQuestion,
        [Description("User - Incident")] [EnumMember] UserIncident,
        [Description("Service Task")] [EnumMember] ServiceTask
    }
}