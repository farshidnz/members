using System;
using System.Collections.Generic;
using System.Text;

namespace SettingsAPI.Model.Dto
{
    public class MembershipAvailableBalance
    {
        public int MemberId { get; set; }
        public decimal? AvailableBalance { get; set; }
    }
}
