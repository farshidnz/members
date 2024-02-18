using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SettingsAPI.EF
{
    public class Community
    {
        [Key] public int CommunityId { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public string AccessCode { get; set; }
        public virtual ICollection<CommunityMemberMap> CommunityMemberMaps { get; set; }
    }
}