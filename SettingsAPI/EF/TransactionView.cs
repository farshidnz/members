using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    [Table("TransactionViewForMember")]
    public partial class TransactionView
    {
        [Key] public int TransactionId { get; set; }
        public int MemberId { get; set; }
        public string TransactionDisplayId { get; set; }
        public DateTime SaleDate { get; set; }
        public string TransactionStatus { get; set; }
        public string MerchantName { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal? SaleValue { get; set; }
        public string CurrencyCode { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal? MemberCommissionValueAUD { get; set; }
        public int ApprovalWaitDays { get; set; }
        public int IsApproved { get; set; }
        public int IsPending { get; set; }
        public int IsDeclined { get; set; }
        public int IsPaid { get; set; }
        public int IsPaymentPending { get; set; }
        public int MerchantId { get; set; }
        public Nullable<int> IsConsumption { get; set; }
        public string DateApproved { get; set; }
        public int TransactionType { get; set; }
        public string OrderId { get; set; }
        public int NetworkId { get; set; }
        public Nullable<int> TransactionTypeId { get; set; }
        public string MaxStatus { get; set; }
        public DateTime SaleDateAest { get; set; }
    }
}