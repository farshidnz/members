using SettingsAPI.Helper;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    [Table("MaterialisedOfferView")]
    public class OfferView
    {
        [Key]
        public int OfferId { get; set; }

        public int ClientId { get; set; }
        public int MerchantId { get; set; }
        public bool IsFeatured { get; set; }
        public string CouponCode { get; set; }
        public string OfferTitle { get; set; }
        public string AlteredOfferTitle { get; set; }
        public string MerchantTrackingLink { get; set; }
        public string OfferDescription { get; set; }
        public string TrackingLink { get; set; }
        public string HyphenatedString { get; set; }
        public System.DateTime DateEnd { get; set; }
        public string CaptionCssClass { get; set; }
        public string MerchantName { get; set; }
        public string RegularImageUrl { get; set; }
        public string BasicTerms { get; set; }
        public string SmallImageUrl { get; set; }
        public string MediumImageUrl { get; set; }
        public Nullable<int> OfferCount { get; set; }
        public int TierCommTypeId { get; set; }
        public int TierTypeId { get; set; }
        public decimal Commission { get; set; }
        public decimal ClientComm { get; set; }
        public decimal ClientCommission => Commission * (ClientComm / 100) * (MemberComm / 100);


        public string ClientCommissionString => string.IsNullOrEmpty(_clientCommissioSting) ? _clientCommissioSting = CommissionHelper.GetCommissionString(ClientProgramTypeId, TierCommTypeId, ClientCommission, Rate, IsFlatRate, TierTypeId, RewardName) : _clientCommissioSting;

        private string _clientCommissioSting { get; set; }
        public decimal MemberComm { get; set; }

        public string TierCssClass { get; set; }
        public string ExtentedTerms { get; set; }
        public string RewardName { get; set; }
        public string MerchantShortDescription { get; set; }
        public string MerchantHyphenatedString { get; set; }
        public int NetworkId { get; set; }
        public string TrackingTime { get; set; }
        public string ApprovalTime { get; set; }
        public string OfferTerms { get; set; }
        public int ClientProgramTypeId { get; set; }
        public Nullable<bool> IsFlatRate { get; set; }
        public decimal Rate { get; set; }
        public string NotificationMsg { get; set; }
        public string ConfirmationMsg { get; set; }
        public Nullable<bool> IsToolbarEnabled { get; set; }
        public Nullable<int> TierCount { get; set; }
        public int Ranking { get; set; }
        public Nullable<System.Guid> RandomNumber { get; set; }
        public string MerchantMetaDescription { get; set; }
        public string OfferBackgroundImageUrl { get; set; }
        public string OfferBadgeCode { get; set; }
        public bool IsCashbackIncreased { get; set; }
        public string MerchantBadgeCode { get; set; }
        public string OfferPastRate { get; set; }
        public bool IsCategoryFeatured { get; set; }
    }
}