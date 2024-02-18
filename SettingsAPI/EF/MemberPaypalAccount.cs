using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    public class MemberPaypalAccount
    {
        [Key] public int Id { get; set; }
        public int MemberId { get; set; }
        public int StatusId { get; set; }
        public DateTime DateEnabled { get; set; }
        public DateTime? DateDisabled { get; set; }
        [Column(TypeName = "nvarchar(200)")] public string PaypalEmail { get; set; }
        public bool? VerifiedAccount { get; set; }
        [Column(TypeName = "nvarchar(1024)")] public string RefreshToken { get; set; }
    }
}