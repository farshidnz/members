using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class UpdateMobileWithCodeRequest
    {
        [JsonPropertyName("code")] public string Code { get; set; }
        [JsonPropertyName("mobile")] public string Mobile { get; set; }
    }
}