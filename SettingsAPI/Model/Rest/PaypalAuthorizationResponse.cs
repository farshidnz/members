using Newtonsoft.Json;

namespace SettingsAPI.Model.Rest
{
    public class PaypalAuthorizationResponse
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }
    }
}