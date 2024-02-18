using System.Collections.Generic;

namespace SettingsAPI.Model.Dto
{
    public class MemberFavouriteRequest
    {
        public IEnumerable<MemberFavouriteRequestMerchant> Merchants { get; set; }
    }

    public class MemberFavouriteRequestMerchant
    {
        public int MerchantId { get; set; }
        public string HyphenatedString { get; set; }
        public int? SelectionOrder { get; set; }
    }
}
