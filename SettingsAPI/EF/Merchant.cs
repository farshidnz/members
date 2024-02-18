using System.ComponentModel.DataAnnotations;

namespace SettingsAPI.EF
{
    public class Merchant
    {
        [Key] public int MerchantId { get; set; }
        public string RegularImageUrl { get; set; }
        public string MerchantName { get; set; }
        public bool? IsPremiumDisabled { get; set; }
        public string HyphenatedString { get; set; }
    }
}