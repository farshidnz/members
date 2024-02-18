using System;
using System.Collections.Generic;
using System.Text;

namespace SettingsAPI.Model.Dto
{
    public class MemberAmountToRedeem
    {
        public int MemberId { get; set; }
        public decimal AmountToRedeem { get; set; }
    }
}
