namespace SettingsAPI.Model.Dto
{
    public class CognitoMemberUpdateEvent
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string PhoneNumber { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PostCode { get; set; }

        public string AccessCode { get; set; }

        public string MemberId { get; set; }

        public string MemberNewId { get; set; }

        public int Status { get; set; }

        public string CognitoId { get; set; }
    }
}