using System.Collections.Generic;
using Newtonsoft.Json;

namespace SettingsAPI.Model.Rest
{
    public class PaypalUserInfoResponse
    {
        [JsonProperty(PropertyName = "emails")]
        public List<Email> Emails { get; set; }

        [JsonProperty(PropertyName = "verified_account")]
        public bool VerifiedAccount { get; set; }

        public class Email
        {
            [JsonProperty(PropertyName = "value")] public string Value { get; set; }

            [JsonProperty(PropertyName = "primary")]
            public bool Primary { get; set; }
        }
    }
}