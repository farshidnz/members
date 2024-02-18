using System.Collections.Generic;
using System.Threading.Tasks;
using SettingsAPI.Model.Dto;

namespace SettingsAPI.Service
{
    public interface IMemberFavouriteService
    {
        public Task<bool> IsFavouriteMarchantAsync(int memberId, int merchantId);
        public Task AddFavouriteAsync(int memberId, MemberFavouriteRequestMerchant request);
        public Task SetFavouritesAsync(int memberId, MemberFavouriteRequest request);
        public Task RemoveFavouriteAsync(int memberId, int merchantId);
        public Task<List<MerchantResult>> GetAllFavouriteAsync(int memberId, string cognitoId);
    }
}
