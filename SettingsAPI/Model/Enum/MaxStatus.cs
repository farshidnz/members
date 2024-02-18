using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SettingsAPI.Model.Enum
{
    public enum MaxStatusEnum
    {
        [Description("Active")] Active = 1,
        [Description("Pending")] Pending = 2,
        [Description("NotApplicable")] NotApplicable = 3,
    }
}
