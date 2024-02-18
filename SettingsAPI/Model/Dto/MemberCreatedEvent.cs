namespace SettingsAPI.Model.Dto
{
    public class MemberCreatedEvent
    {
        public int MemberId { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool ReceiveNewsLetter { get; set; }

        public bool IsValidated { get; set; }

        public string ExternalMemberId { get; set; }

        public string MobileNumber { get; set; }

        public string ReferralSource { get; set; }

        public int ClientId { get; set; }

        public int PremiumStatus { get; set; }

        public string OriginationSource { get; set; }

        public string PlatformJoined { get; set; }
    }
}