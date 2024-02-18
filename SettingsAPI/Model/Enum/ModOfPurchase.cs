using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum ModOfPurchase
    {
        [Description("Online")] [EnumMember] Online,
        [Description("In-store")] [EnumMember] InStore
    }
}