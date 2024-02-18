using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum MergeLanguage
    {
        [Description("handlebars")] [EnumMember] HandleBars = 0,
        [Description("mailchimp")] [EnumMember] MailChimp = 1
    }
}