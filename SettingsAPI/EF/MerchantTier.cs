using System;
using System.ComponentModel.DataAnnotations;

namespace SettingsAPI.EF
{
    public class MerchantTier
    {
        [Key] public int MerchantTierId { get; set; }
        public string TierName { get; set; }
        public string TierReference { get; set; }
    }
}