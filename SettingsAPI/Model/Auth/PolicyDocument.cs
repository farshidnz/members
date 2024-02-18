using Newtonsoft.Json;
using System.Collections.Generic;

namespace SettingsAPI.Model.Auth
{
    public class PolicyDocument
    {
        [JsonProperty(PropertyName = "Version")]
        public string Version { get; set; }
        [JsonProperty(PropertyName = "Statement")]
        public IEnumerable<Statement> Statement { get; set; }
    }
}
