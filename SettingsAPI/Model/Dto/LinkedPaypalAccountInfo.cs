using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Dto
{
    public class LinkedPaypalAccountInfo
    {
        [JsonPropertyName("paypalEmail")]public string PaypalEmail { get; set; }
    }
}