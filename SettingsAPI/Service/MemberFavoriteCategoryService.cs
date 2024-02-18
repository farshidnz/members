using Microsoft.EntityFrameworkCore;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Model.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class MemberFavouriteCategoryService : IMemberFavouriteCategoryService
    {
        private readonly ShopGoContext _context;

        public MemberFavouriteCategoryService(ShopGoContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GetFavouriteCategoriesAsync(int memberId)
        {
            return await _context.MemberFavouriteCategory.Where(mf => mf.MemberId == memberId)
                .Select(mf => mf.CategoryId)
                .ToListAsync();
        }

        public async Task<bool> IsFavouriteCategoryAsync(int memberId, int categoryId)
        {
            var memberFavourite = await _context.MemberFavouriteCategory
                .Where(mf => mf.MemberId == memberId && mf.CategoryId == categoryId).AsNoTracking()
                         .FirstOrDefaultAsync();
            return memberFavourite != null;
        }

        public async Task SetFavouriteCategoriesAsync(int memberId, int[] categoryIds)
        {
            if (categoryIds == null) return;

            var toRemove = await _context.MemberFavouriteCategory.Where(mf => mf.MemberId == memberId)
                .ToListAsync();

            var toAdd = categoryIds
                .Select(c => new MemberFavouriteCategory()
            {
                MemberId = memberId,
                CategoryId = c,
                DateCreated = DateTime.Now
            }).ToList();

            if (toRemove.Any())
            {
                _context.MemberFavouriteCategory.RemoveRange(toRemove);
            }

            if (toAdd.Any())
            {
                _context.MemberFavouriteCategory.AddRange(toAdd);
            }

            await _context.SaveChangesAsync();
            
        }
    }
}