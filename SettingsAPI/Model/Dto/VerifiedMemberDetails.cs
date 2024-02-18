using SettingsAPI.EF;
using System;
using System.Collections.Generic;
using System.Text;

namespace SettingsAPI.Model.Dto
{
    public class VerifiedMemberDetails
    {
        public MembershipDetail MembershipInfo { get; set; }
        public Member MemberInfo { get; set; }
        public IEnumerable<MembershipAvailableBalance> AvailableBalances { get; set; }

        public void Deconstruct(out MembershipDetail membershipInfo, out Member memberStore, out IEnumerable<MembershipAvailableBalance> membershipBalances)
        {
            membershipInfo = MembershipInfo;
            memberStore = MemberInfo;
            membershipBalances = AvailableBalances;
        }

    }
}
