using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum PurchaseApproved
    {
        [Description("Yes")] [EnumMember] Yes,
        [Description("No")] [EnumMember] No
    }
}