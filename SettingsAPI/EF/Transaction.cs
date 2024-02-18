using System.ComponentModel.DataAnnotations;

namespace SettingsAPI.EF
{
    public class Transaction
    {
        [Key] public int TransactionId { get; set; }
        public int MemberId { get; set; }
        
        public int MerchantId { get; set; }
        
        public int TransactionStatusId { get; set; }
        
        public int? TransactionTypeId { get; set; }
    }
}