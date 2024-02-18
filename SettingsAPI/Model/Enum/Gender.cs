using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum Gender
    {
        [Description("Male")] [EnumMember] Male = 1,
        [Description("Female")] [EnumMember] Female = 2,
        [Description("Other")] [EnumMember] Other = 0,

        [Description("Prefer not to say")] [EnumMember]
        PreferNotToSay = 3
    }
}