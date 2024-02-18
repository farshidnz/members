using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum PremiumStatusEnum
    {
        NotEnrolled = 0,
        Enrolled = 1,
        OptOut = 999
    }

    /**
     *
     * This enum defines property config on freshdesk
     */
    public enum PremiumStatusTicketEnum
    {
        [Description("Not yet enrolled to premium program")] [EnumMember] NotEnrolled,
        [Description("Opted in to premium program")] [EnumMember] Enrolled,
        [Description("Opted out to premium program")] [EnumMember] OptOut
    }
}