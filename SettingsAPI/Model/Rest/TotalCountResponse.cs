using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class TotalCountResponse
    {
        [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    }
}