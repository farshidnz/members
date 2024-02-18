using SettingsAPI.Model.Enum;
using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class MemberDetailsRequest
    {
        [JsonPropertyName("firstName")] public string FirstName { get; set; }
        [JsonPropertyName("lastName")] public string LastName { get; set; }
        [JsonPropertyName("dateOfBirth")] public string DateOfBirth { get; set; }
        [JsonPropertyName("gender")] public string Gender { get; set; }
        [JsonPropertyName("postCode")] public string PostCode { get; set; }
        [JsonPropertyName("mobileOtp")] public string MobileOtp { get; set; }
    }
}