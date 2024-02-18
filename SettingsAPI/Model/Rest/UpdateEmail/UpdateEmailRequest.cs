namespace SettingsAPI.Model.Rest.UpdateEmail
{
    public class EmailUpdateRequest
    {
        public string Email { get; set; }

        public string MobileOtp { get; set; }
    }
}