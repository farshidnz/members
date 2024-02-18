using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Dto
{
    public class MemberBankAccountInfo
    {
        [JsonPropertyName("accountName")] public string AccountName { get; set; }
        [JsonPropertyName("bsb")] public string Bsb { get; set; }
        [JsonPropertyName("accountNumber")] public string AccountNumber { get; set; }
    }
}