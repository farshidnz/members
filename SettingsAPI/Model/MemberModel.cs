namespace SettingsAPI.Model
{
    public class MemberModel
    {
        public int MemberId { get; set; }
        public string MobileOtp { get; set; }
        public string MobileNumber { get; set; }

        public int PersonId { get; set; }

        public string Email { get; set; }
    }
}