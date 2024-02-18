using System;
using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Dto
{
    public class MerchantResult
    {
        public string MerchantName { get; set; }
        public int MerchantId { get; set; }
        public string HyphenatedString { get; set; }
        public string RegularImageUrl { get; set; }
        public Nullable<bool> IsOnline { get; set; }
        public Nullable<bool> IsTrueRewards { get; set; }
        public string ClientCommissionString { get; set; }
        [JsonIgnore]
        public int NetworkId { get; set; }
        public decimal ClientCommission { get; set; }

        public PremiumDto Premium { get; set; }

    }
}
