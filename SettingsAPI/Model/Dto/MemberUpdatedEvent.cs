namespace SettingsAPI.Model.Dto
{
    public class MemberUpdatedEvent
    {
        public int MemberId { get; set; }

        public string Email { get; set; }

        public string Old_Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool? ReceiveNewsLetter { get; set; }

        public int Status { get; set; }

        public bool IsValidated { get; set; }

        public string ExternalMemberId { get; set; }

        public string MobileNumber { get; set; }

        public string ReferralSource { get; set; }

        public string PlatformJoined { get; set; }

        public int ClientId { get; set; }

        public bool? AppNotificationConsent { get; set; }
        public bool SmsConsent { get; set; }
    }
}