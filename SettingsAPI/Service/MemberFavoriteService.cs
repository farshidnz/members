using Microsoft.EntityFrameworkCore;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Model.Dto;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class MemberFavoriteService : IMemberFavouriteService
    {
        private readonly ShopGoContext _context;

        private readonly ReadOnlyShopGoContext _readOnlyContext;

        private readonly IPremiumService _premiumService;

        private readonly IFeatureToggleService _featureToggleService;

        public MemberFavoriteService(ShopGoContext context, ReadOnlyShopGoContext readOnlyContext, IPremiumService premiumService, IFeatureToggleService featureToggleService)
        {
            _context = context;
            _readOnlyContext = readOnlyContext;
            _premiumService = premiumService;
            _featureToggleService = featureToggleService;
        }

        public async Task AddFavouriteAsync(int memberId, MemberFavouriteRequestMerchant request)
        {
            var memberFavourite = await _context.MemberFavourite
                .Where(mf => mf.MemberId == memberId && mf.MerchantId == request.MerchantId).AsNoTracking()
                .FirstOrDefaultAsync();

            if (memberFavourite == null)
            {
                var toAdd = await ExcludeInvalid(new MemberFavourite
                {
                    MemberId = memberId,
                    MerchantId = request.MerchantId,
                    HyphenatedString = request.HyphenatedString,
                    DateCreated = DateTime.Now
                });

                if (toAdd.Any())
                {
                    var nextSelectionOrder = (_context.MemberFavourite.Where(mf => mf.MemberId == memberId)
                        .OrderByDescending(mf => mf.SelectionOrder).FirstOrDefault()?.SelectionOrder ?? -1) + 1;

                    toAdd[0].SelectionOrder = nextSelectionOrder;
                    _context.MemberFavourite.AddRange(toAdd);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task SetFavouritesAsync(int memberId, MemberFavouriteRequest request)
        {
            var toRemove = await _context.MemberFavourite.Where(mf => mf.MemberId == memberId)
                .ToListAsync();

            var toAdd = request.Merchants.Select(fav => new MemberFavourite
            {
                MemberId = memberId,
                MerchantId = fav.MerchantId,
                HyphenatedString = fav.HyphenatedString,
                DateCreated = DateTime.Now,
                SelectionOrder = fav.SelectionOrder ?? 0
            }).ToArray();

            toAdd = await ExcludeInvalid(toAdd);

            if (toRemove.Any())
            {
                _context.MemberFavourite.RemoveRange(toRemove);
            }

            if (toAdd.Any())
            {
                _context.MemberFavourite.AddRange(toAdd);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<MemberFavourite[]> ExcludeInvalid(params MemberFavourite[] favourites)
        {
            var favouriteMerchantIds = favourites.Select(f => f.MerchantId).ToArray();
            var merchants = await _context.Merchant.Where(m => favouriteMerchantIds.Contains(m.MerchantId)).ToListAsync();
            return favourites
                .Where(f => merchants.Any(m => m.MerchantId == f.MerchantId && (f.HyphenatedString == null || m.HyphenatedString == f.HyphenatedString)))
                .ToArray();
        }

        private string CaculateCommisionString(decimal commision, int tierCommTypeEnum)
        {
            if (tierCommTypeEnum == 100)
            {
                return String.Format(new System.Globalization.CultureInfo("en-AU"), "{0:C}", commision);
            }
            else
            {
                return $"{decimal.Parse(commision.ToString("F2")):G29}%";
            };
        }

        public async Task<List<MerchantResult>> GetAllFavouriteAsync(int memberId, string cognitoId)
        {
            var premiumMembership = await _premiumService.GetPremiumMembership(Constant.Clients.CashRewards, cognitoId);
            var premiumClientId = premiumMembership?.IsCurrentlyActive ?? false ? premiumMembership?.PremiumClientId : null;

            var memberFavourites = _readOnlyContext.MemberFavourite.Where(mf => mf.MemberId == memberId).AsNoTracking();

            List<MerchantView> merchants = await _readOnlyContext.MerchantView
                .Where(mt => mt.ClientId == Constant.Clients.CashRewards && memberFavourites.Select(m => m.MerchantId).Contains(mt.MerchantId))
                .AsNoTracking()
                .ToListAsync();

            var premiumMerchants = new Dictionary<int, MerchantView>();
            if (premiumClientId.HasValue)
            {
                merchants = merchants.Where(m => !m.IsPremiumDisabled.GetValueOrDefault(false)).ToList();
                List<int> merchantIds = merchants.Select(m => m.MerchantId).ToList();
                var premiumMerchantList = await _readOnlyContext.Set<MerchantView>()
                    .Where(c => c.ClientId == premiumClientId)
                    .Where(m => merchantIds.Contains(m.MerchantId)).ToListAsync();
                premiumMerchants = premiumMerchantList.ToDictionary(m => m.MerchantId, m => m);
            }

            var dict = new Dictionary<string, MerchantResult>();
            foreach (var element in merchants)
            {
                var isOnline = element.NetworkId != 1000059 && element.NetworkId != 1000053;
                var commission = element.Commission * (element.ClientComm / 100) * (element.MemberComm / 100);
                var merchantResult = dict.GetValueOrDefault(element.MerchantId.ToString(), new MerchantResult
                {
                    MerchantId = element.MerchantId,
                    HyphenatedString = element.HyphenatedString,
                    MerchantName = element.MerchantName,
                    RegularImageUrl = element.RegularImageUrl,
                    ClientCommission = commission,
                    NetworkId = element.NetworkId,
                    IsOnline = isOnline,
                    IsTrueRewards = element.NetworkId == 1000063,
                });

                merchantResult.MerchantName = merchantResult.MerchantName;

                // Online Merchant
                if (merchantResult.IsOnline == false && isOnline)
                {
                    merchantResult.IsOnline = true;
                }
                if (merchantResult.ClientCommission != commission)
                {
                    merchantResult.ClientCommission = commission > merchantResult.ClientCommission ? commission : merchantResult.ClientCommission;
                }
                merchantResult.ClientCommissionString = GetMerchantResultCommissionString(merchantResult.ClientCommission, element);
                if (string.IsNullOrEmpty(merchantResult.HyphenatedString))
                {
                    merchantResult.HyphenatedString = element.HyphenatedString;
                }

                if (premiumClientId.HasValue && premiumMerchants.TryGetValue(element.MerchantId, out var premiumMerchant))
                {
                    (var premiumCommission, var premiumClientCommissionString) = GetMerchantResultCommission(premiumMerchant);
                    merchantResult.Premium = new PremiumDto
                    {
                        Commission = premiumCommission,
                        IsFlatRate = (bool)premiumMerchant.IsFlatRate,
                        ClientCommissionString = premiumClientCommissionString
                    };
                }

                if (_featureToggleService.IsEnable(FeatureFlags.IS_MERCHANT_PAUSED) && element.IsPaused)
                {
                    merchantResult.ClientCommission = 0;
                    merchantResult.ClientCommissionString = "No current offers";
                    if(merchantResult.Premium!=null)
                    {
                        merchantResult.Premium.Commission = 0;
                        merchantResult.Premium.ClientCommissionString = "No current offers";
                    }
                }

                dict[element.MerchantId.ToString()] = merchantResult;
            }

            var favourites = memberFavourites.ToDictionary(k => k.MerchantId, v => v.SelectionOrder);

            return dict.Values.OrderBy(v => favourites[v.MerchantId]).ToList();
        }

        private (decimal commission, string clientCommissionString) GetMerchantResultCommission(MerchantView merchantView)
        {
            var commission = merchantView.Commission * (merchantView.ClientComm / 100) * (merchantView.MemberComm / 100);
            return (commission, GetMerchantResultCommissionString(commission, merchantView));
        }

        private string GetMerchantResultCommissionString(decimal commission, MerchantView merchantView)
        {
            if (commission == 0)
            {
                return "No current offers";
            }

            var clientCommissionString = new StringBuilder();
            if (merchantView.TierCount > 1)
            {
                clientCommissionString.Append("Up to ");
            }
            clientCommissionString.Append(CaculateCommisionString(commission, merchantView.TierCommTypeId));
            clientCommissionString.Append(" cashback");

            return clientCommissionString.ToString();
        }

        async Task<bool> IMemberFavouriteService.IsFavouriteMarchantAsync(int memberId, int merchantId)
        {
            var memberFavourite = await _context.MemberFavourite.Where(mf => mf.MemberId == memberId && mf.MerchantId == merchantId).AsNoTracking()
                         .FirstOrDefaultAsync();
            return memberFavourite != null;
        }

        async Task IMemberFavouriteService.RemoveFavouriteAsync(int memberId, int merchantId)
        {
            var memberFavourite = await _context.MemberFavourite.Where(mf => mf.MemberId == memberId && mf.MerchantId == merchantId)
                        .FirstOrDefaultAsync();

            if (memberFavourite != null)
            {
                _context.MemberFavourite.Remove(memberFavourite);
                await _context.SaveChangesAsync();
            }
        }
    }
}