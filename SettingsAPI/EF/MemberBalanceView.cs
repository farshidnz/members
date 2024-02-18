using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    public class MemberBalanceView
    {
        [Key] public int MemberID { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal? TotalBalance { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal? AvailableBalance { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal? LifetimeRewards { get; set; }
        [Column(TypeName = "decimal(19, 4)")] public decimal? RedeemBalance { get; set; }
    }
}