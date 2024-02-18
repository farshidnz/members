using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class CommsPreferencesRequest
    {
        [JsonPropertyName("subscribeNewsletters")]
        public bool? SubscribeNewsletters { get; set; }

        [JsonPropertyName("subscribeSMS")]
        public bool? SubscribeSMS { get; set; }

        [JsonPropertyName("subscribeAppNotifications")]
        public bool? SubscribeAppNotifications { get; set; }
    }
}