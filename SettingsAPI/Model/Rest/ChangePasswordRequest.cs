using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class ChangePasswordRequest
    {
        [JsonPropertyName("newPassword")] public string NewPassword { get; set; }
        [JsonPropertyName("mobileOtp")] public string MobileOtp { get; set; }
    }
}