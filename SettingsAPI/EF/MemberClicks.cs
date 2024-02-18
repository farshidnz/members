#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    public class MemberClicks
    {
        [Key] public long ClickId { get; set; }

        public int MemberId { get; set; }

        public int MerchantId { get; set; }

        public string? ItemType { get; set; }

        public int ItemId { get; set; }

        public DateTime DateCreated { get; set; }

        [Column(TypeName = "varchar(500)")] public string? IpAddress { get; set; }

        [Column(TypeName = "varchar(1000)")] public string? RedirectionLinkUsed { get; set; }

        public bool AdBlockerEnabled { get; set; }

        public string? UserAgent { get; set; }

        public string? CashbackOffer { get; set; }

        public int CampaignId { get; set; }

        public DateTime SysStartTime { get; set; }

        public DateTime SysEndTime { get; set; }
        
        public DateTime DateCreatedUtc { get; set; }
    }
}