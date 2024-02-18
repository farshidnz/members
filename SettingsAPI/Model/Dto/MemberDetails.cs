namespace SettingsAPI.Model.Dto
{
    public class MemberDetails
    {
        public string NewMemberId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public decimal? Balance { get; set; }
        public int MemberId { get; set; }
        public string Email { get; set; }
        public int IsRisky { get; set; }
        public decimal? LifetimeRewards { get; set; }
        public string Mobile { get; set; }
        public string PostCode { get; set; }
        public string Gender { get; set; }

        public int DaysFromJoined { get; set; }

        public MemberInfoWelcomeBonus WelComeBonus { get; set; }

        public string Comment { get; set; }

        public int ClientId { get; set; }

        public decimal? AvailableBalance { get; set; }

        public decimal? RedeemBalance { get; set; }

        public string DateOfBirth { get; set; }

        public bool? ReceiveNewsLetter { get; set; }

        public bool IsValidated { get; set; }

        public bool InstallNotifier { get; set; }

        public bool ShowCommunicationsPrompt { get; set; }

        public bool IsPremium { get; set; }

        public int PremiumStatus { get; set; }
    }

    public class MemberInfoWelcomeBonus
    {
        public decimal? Amount { get; set; }

        public bool IsRedeemed { get; set; }
    }
}