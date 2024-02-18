using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class SendMailAfterWithDrawRequest
    {
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
    }
}