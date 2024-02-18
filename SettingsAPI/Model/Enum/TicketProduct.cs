using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum TicketProduct
    {
        [Description("Cashrewards")] [EnumMember] Cashrewards = 1000001116,
        [Description("Development")] [EnumMember] Development = 1000001118,
        [Description("Infrastructure")] [EnumMember] Infrastructure	 = 1000005630,
        [Description("Max")] [EnumMember] Max = 1000007491,
        [Description("MoneyMe")] [EnumMember] MoneyMe = 1000007415,
        [Description("Operations")] [EnumMember] Operations = 1000005627,
        [Description("Pink")] [EnumMember] Pink = 1000005035,
        [Description("ShopGo")] [EnumMember] ShopGo = 1000001117
    }
}