using System.Text.Json.Serialization;

namespace SettingsAPI.Model.Dto
{
    public class MemberCommsPreferencesInfo
    {
        public bool? SubscribeNewsletters { get; set; }
        public bool SubscribeSMS { get; set; }
        public bool? SubscribeAppNotifications { get; set; }
    }
}