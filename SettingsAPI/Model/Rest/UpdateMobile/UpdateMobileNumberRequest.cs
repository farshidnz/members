using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest.UpdateMobile
{
    public class UpdateMobileNumberRequest
    {
        public string MobileNumber { get; set; }
        
        public string MobileOtp { get; set; }
    }
}