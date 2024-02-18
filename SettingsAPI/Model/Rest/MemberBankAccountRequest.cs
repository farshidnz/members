using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class MemberBankAccountRequest
    {
        [JsonPropertyName("accountName")] public string AccountName { get; set; }
        [JsonPropertyName("bsb")] public string Bsb { get; set; }
        [JsonPropertyName("accountNumber")] public string AccountNumber { get; set; }
        
        [JsonPropertyName("mobileOtp")] public string MobileOtp { get; set; }
    }
}