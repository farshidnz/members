using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace SettingsAPI.Model.Enum
{
    public enum CommsPromptDismissalAction
    {
        [Description("Close")]
        [EnumMember]
        Close,

        [Description("Review")]
        [EnumMember]
        Review
    }
}
