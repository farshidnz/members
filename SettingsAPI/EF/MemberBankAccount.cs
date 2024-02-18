using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    public class MemberBankAccount
    {
        [Key] public int BankAccountId { get; set; }

        public int MemberId { get; set; }
        public int Status { get; set; }
        [Column(TypeName = "varchar(100)")] public string AccountName { get; set; }
        [Column(TypeName = "varchar(10)")] public string Bsb { get; set; }
        [Column(TypeName = "varchar(20)")] public string AccountNumber { get; set; }
        [Column(TypeName = "varchar(50)")] public string BankName { get; set; }
        public DateTime DateCreated { get; set; }
    }
}