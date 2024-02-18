using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum QueryConditionFields
    {
        [Description("Order by")] [EnumMember] OrderBy = 0,

        [Description("Sort direction")] [EnumMember]
        SorDirection = 1,

        [Description("Date from")] [EnumMember]
        DateFrom = 2,
        [Description("Date to")] [EnumMember] DateTo = 3,
        [Description("Offset")] [EnumMember] Offset = 4,
        [Description("Limit")] [EnumMember] Limit = 5
    }
}