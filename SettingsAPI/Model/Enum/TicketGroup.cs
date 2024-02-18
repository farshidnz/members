using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TicketGroup
    {
        [Description("ANZ Pilot")] [EnumMember] ANZPilot = 1000202530,
        [Description("Approvals")] [EnumMember] Approvals = 1000198716,
        [Description("BWS Tracking")] [EnumMember] BWSTracking	= 1000201747,
        [Description("Cashback Claims")] [EnumMember] CashbackClaims = 1000197669,
        [Description("Cashrewards")] [EnumMember] Cashrewards = 1000016657,
        [Description("Facebook")] [EnumMember] Facebook = 1000198993,
        [Description("General Support")] [EnumMember] GeneralSupport = 1000016640,
        [Description("In-App Feedback")] [EnumMember] InAppFeedback = 1000202531,
        [Description("Incorrectly Declined")] [EnumMember] IncorrectlyDeclined = 1000198718,
        [Description("Incorrectly Tracked")] [EnumMember] IncorrectlyTracked = 1000198717,
        [Description("Partners")] [EnumMember] Partners = 1000201661,
        [Description("Payment Support")] [EnumMember] PaymentSupport = 1000016658,
        [Description("TQ Follow Ups")] [EnumMember] TQFollowUps = 1000198658,
        [Description("Twitter")] [EnumMember] Twitter = 1000198802,
        [Description("Urgent Tickets")] [EnumMember] UrgentTickets = 1000198657
    }
}