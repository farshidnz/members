using System;
using System.Collections.Generic;
using System.Text;

namespace SettingsAPI.Model
{
    public class FieldAudit
    {
        public string FieldName { get; set; }
        public string FromValue { get; set; }
        public string ToValue { get; set; }
    }
}
