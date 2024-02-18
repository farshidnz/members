using System;
using System.ComponentModel.DataAnnotations;

namespace SettingsAPI.EF
{
    public class CommunityMemberMap
    {
        [Key] public int CommunityMemberMapId { get; set; }
        public int MemberId { get; set; }
    }
}