using System.Collections.Generic;
using System.Threading.Tasks;
using SettingsAPI.Model.Dto;

namespace SettingsAPI.Service
{
    public interface IMemberFavouriteCategoryService
    {
        public Task<bool> IsFavouriteCategoryAsync(int memberId, int categoryId);
        public Task SetFavouriteCategoriesAsync(int memberId, int[] categoryIds);
        public Task<List<int>> GetFavouriteCategoriesAsync(int memberId);
    }
}
