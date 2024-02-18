using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SettingsAPI.EF
{
    public class MemberFavourite
    {
        [Key]
        [JsonIgnore]
        public long MemberFavouriteId { get; set; }
        [JsonIgnore]
        public int MemberId { get; set; }
        public int MerchantId { get; set; }
        public string HyphenatedString { get; set; }
        public int SelectionOrder { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
