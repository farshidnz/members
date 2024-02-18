using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum ChangeAction
    {
        [Description("Add")] [EnumMember] Add = 1,
        [Description("Remove")] [EnumMember] Remove = 2
    }
}