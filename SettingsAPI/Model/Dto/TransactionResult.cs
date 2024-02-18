using System;

namespace SettingsAPI.Model.Dto
{
    public class TransactionResult
    {
        public int TransactionId { get; set; }

        public DateTime SaleDate { get; set; }

        public string TransactionName { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public decimal Commission { get; set; }

        public string Status { get; set; }

        public string TransactionType { get; set; }

        public string EstimatedApprovalDate { get; set; }

        public bool IsConsumption { get; set; }

        public string RegularImageUrl { get; set; }

        public int NetworkId { get; set; }

        public bool IsOverdue { get; set; }
        public string CashbackFlag { get; set; }
        public string MaxStatus { get; set; }
        public DateTime SaleDateAest { get; set; }
    }
}