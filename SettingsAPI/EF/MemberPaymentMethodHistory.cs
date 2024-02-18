using System;
using System.ComponentModel.DataAnnotations;

namespace SettingsAPI.EF
{
    public class MemberPaymentMethodHistory
    {
        [Key] public int Id { get; set; }
        public int MemberId { get; set; }
        public int AccountType { get; set; }
        public int AccountId { get; set; }
        public string HashedValue { get; set; }
        public int ChangeType { get; set; }
        public DateTime DateChanged { get; set; }
        public virtual Member Member { get; set; }
    }
}