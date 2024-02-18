using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class WithdrawRequest
    {
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("paymentMethod")] public string PaymentMethod { get; set; }

        [JsonPropertyName("mobileOtp")] public string MobileOtp { get; set; }
    }
}