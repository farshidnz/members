using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    public class TransactionTier
    {
        [Key] public int TransactionTierId { get; set; }
        public int TransactionId { get; set; }
        
        public int? MerchantTierId { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal MemberCommissionValueAud { get; set; }
    }
}