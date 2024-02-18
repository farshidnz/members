using System;
using System.Collections.Generic;
using System.Text;

namespace SettingsAPI.Model.Rest
{
    public class UpdateCommsPreferencesModel : BaseModel
    {
        public bool? SubscribeSMS { get; set; }
        public bool? SubscribeNewsletters { get; set; }
        public bool? SubscribeAppNotifications { get; set; }
        
    }
}
