using SettingsAPI.Model.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace SettingsAPI.Model.Rest
{
    public class CommsPromptShownModel : BaseModel
    {
        public CommsPromptDismissalAction Action { get; set; }
    }
}
