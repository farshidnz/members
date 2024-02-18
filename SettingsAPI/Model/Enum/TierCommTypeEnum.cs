using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TierCommTypeEnum
    {
        [Description("Percentage")]
        [EnumMember]
        Percentage = 101,

        [Description("Dollar")]
        [EnumMember]
        Dollar = 100
    }
}