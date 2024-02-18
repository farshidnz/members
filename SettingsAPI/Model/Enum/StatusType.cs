using System.ComponentModel;
using System.Runtime.Serialization;


namespace SettingsAPI.Model.Enum
{
    public enum StatusType
    {
        [Description("Deleted")]
        [EnumMember] 
        Deleted = 0,
        [Description("Active")] 
        [EnumMember] 
        Active = 1,
        [Description("Inactive")] 
        [EnumMember] 
        Inactive = 2,
        [Description("Unverified")] 
        [EnumMember] 
        Unverified = 3,
        [Description("")] 
        [EnumMember] 
        NotAssigned = 100
    }
}