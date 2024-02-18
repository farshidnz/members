using SettingsAPI.Model.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SettingsAPI.Common.Constant;

namespace SettingsAPI.Model.Dto
{
    public class MembershipDetail
    {
        public List<MemberShipItem> Items { get; set; }

        private int? _premiumStatus;

        private DateTime? _premiumDateJoined;

        public int PremiumStatus
        {
            get 
            { 
                _premiumStatus = _premiumStatus ?? Items.FirstOrDefault()?.PremiumStatus ?? (int)PremiumStatusEnum.NotEnrolled;
                return (int)_premiumStatus;
            }
        }

        public DateTime? PremiumDateJoined
        {
            get 
            {
                _premiumDateJoined = _premiumDateJoined?? Items.FirstOrDefault(m => m.ClientId == Clients.ANZ)?.DateJoined;
                return _premiumDateJoined;
            }
        }

        public bool IsPremium
        {
            get { return PremiumStatus == (int)PremiumStatusEnum.Enrolled; }
        }
    }
    
    public class MemberShipItem
    {
        public int MemberId { get; set; }

        public int ClientId { get; set; }

        public int? PersonId { get; set; }

        public int PremiumStatus { get; set; }

        public DateTime? DateJoined { get; set; }
    }
}
