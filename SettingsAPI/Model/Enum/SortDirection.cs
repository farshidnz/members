using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum SortDirection
    {
        [Description("Asc")] [EnumMember] Asc = 0,
        [Description("Desc")] [EnumMember] Desc = 1
    }
}