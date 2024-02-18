using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Rest
{
    public class SmsOptOutRequest
    {
        [JsonPropertyName("memberId")] public int MemberId { get; set; }
        [JsonPropertyName("key")] public string Key { get; set; }
    }
}
