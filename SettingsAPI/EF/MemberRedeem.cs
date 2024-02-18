using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    public class MemberRedeem
    {
        [Key] public int RedeemId { get; set; }
        public int MemberId { get; set; }
        public int PaymentStatusId { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal AmountRequested { get; set; }
        public DateTime DateRequested { get; set; }
        public int PaymentMethodId { get; set; }
        [Column(TypeName = "nvarchar(100)")] public string PaymentMethodReference { get; set; }
        [Column(TypeName = "datetime2")] public DateTime DateRequestedUtc { get; set; }
        public string PaymentMethodDetail { get; set; }
        public string WithdrawalId { get; set; }
        public bool IsPartial { get; set; }
    }
}