using System;

namespace SettingsAPI.Model.Dto
{
    public class MemberClicksHistoryResult
    {
        public long ClickId { get; set; }
        public DateTime Date { get; set; }
        public string Store { get; set; }
        public string RegularImageUrl { get; set; }
    }
}