using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum ClientProgramTypeEnum
    {
        [Description("CashProgram")]
        [EnumMember]
        CashProgram = 100,

        [Description("ProductProgram")]
        [EnumMember]
        ProductProgram = 101,

        [Description("PointsProgram")]
        [EnumMember]
        PointsProgram = 102,
    }
}