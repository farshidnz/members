using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class PaypalConnectUrlInfo
    {
        [JsonPropertyName("paypalConnectUrl")] public string PaypalConnectUrl { get; set; }
    }
}