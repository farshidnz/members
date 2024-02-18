using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class FeedbackRequest
    {
        [JsonPropertyName("feedback")] public string Feedback { get; set; }
        [JsonPropertyName("appVersion")] public string AppVersion { get; set; }
        [JsonPropertyName("deviceModel")] public string DeviceModel { get; set; }
        [JsonPropertyName("operatingSystem")] public string OperatingSystem { get; set; }
        
        [JsonPropertyName("buildNumber")] public string BuildNumber { get; set; }
    }
}