using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class CommsPromptShownRequest 
    {         
        [JsonPropertyName("action")]
        public string Action { get; set; }
    }
}
