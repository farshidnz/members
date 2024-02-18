using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    [Table("MaterialisedMerchantView")]
    public class MerchantView
    {
        
        public string DescriptionShort { get; set; }
        public string DescriptionLong { get; set; }
        public string BasicTerms { get; set; }
        public string ExtentedTerms { get; set; }
        public int MerchantId { get; set; }
        public Nullable<bool> IsLatest { get; set; }
        public int NetworkId { get; set; }
        public string MerchantName { get; set; }
        public Nullable<bool> IsFeatured { get; set; }
        public Nullable<bool> IsPopular { get; set; }
        public Nullable<bool> IsHomePageFeatured { get; set; }
        public string HyphenatedString { get; set; }
        public string RegularImageUrl { get; set; }
        public string SmallImageUrl { get; set; }
        public string MediumImageUrl { get; set; }
        public string RegularImageUrlSecure { get; set; }
        public string SmallImageUrlSecure { get; set; }
        public string MediumImageUrlSecure { get; set; }
        public Nullable<System.Guid> RandomNumber { get; set; }
        public int ClientId { get; set; }
        public int TierCommTypeId { get; set; }
        public decimal Commission { get; set; }
        public decimal ClientComm { get; set; }
        public decimal MemberComm { get; set; }
        public int TierTypeId { get; set; }
        public string TierCssClass { get; set; }
        public string TrackingLink { get; set; }
        public Nullable<bool> IsExtra { get; set; }
        public Nullable<bool> IsFlatRate { get; set; }
        public string FlagImageUrl { get; set; }
        public Nullable<int> OfferCount { get; set; }
        public string RewardName { get; set; }
        public int ClientProgramTypeId { get; set; }
        public string TierDescription { get; set; }
        public string TierName { get; set; }
        public string WebsiteUrl { get; set; }
        public string TrackingTime { get; set; }
        public string ApprovalTime { get; set; }
        public decimal Rate { get; set; }
        public string NotificationMsg { get; set; }
        public string ConfirmationMsg { get; set; }
        public Nullable<bool> MobileEnabled { get; set; }
        public bool DesktopEnabled { get; set; }
        public Nullable<int> TierCount { get; set; }
        public Nullable<bool> IsToolbarEnabled { get; set; }
        public bool IsLuxuryBrand { get; set; }
        public string MetaDescription { get; set; }
        public Nullable<int> MobileAppTrackingType { get; set; }
        public Nullable<int> MobileTrackingNetwork { get; set; }
        public string ReferenceName { get; set; }
        public Nullable<bool> IsMobileAppEnabled { get; set; }
        public string BackgroundImageUrl { get; set; }
        public string MerchantBadgeCode { get; set; }
        public bool? IsPremiumDisabled { get; set; }
        public bool IsPaused { get; set; }
    }
}
