using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class MemberBalanceService : IMemberBalanceService
    {
        private readonly ReadOnlyShopGoContext _context;
        private readonly IRedisUtil _redisUtil;
        private readonly IFeatureToggleService _featureToggleService;

        public MemberBalanceService(
            ReadOnlyShopGoContext context,
            IRedisUtil redisUtil,
            IFeatureToggleService featureToggleService)
        {
            _context = context;
            _redisUtil = redisUtil;
            _featureToggleService = featureToggleService;
        }

        public async Task<IList<MemberBalanceView>> GetBalanceViews(int[] memberIds, bool useCache)
        {
            if (memberIds == null || memberIds.Length == 0) return null;

            if (useCache && _featureToggleService.IsEnable(FeatureFlags.MEMBER_BALANCE_CACHE))
            {
                var cacheSettings = MemberBalanceViewCacheSettings;
                return await _redisUtil.GetWithConfigurableCache(
                    MemberBalanceViewCacheKey(memberIds),
                    () => _context.MemberBalanceView.Where(mem => memberIds.Contains(mem.MemberID)).ToListAsync(),
                    cacheSettings);
            }
            else
            {
                return await _context.MemberBalanceView.Where(mem => memberIds.Contains(mem.MemberID)).ToListAsync();
            }
        }

        private static string MemberBalanceViewCacheKey(int[] memberIds) => $"MemberBalanceView-{string.Join("-", memberIds)}";

        private CacheSettings MemberBalanceViewCacheSettings
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<CacheSettings>(_featureToggleService.GetVariant(FeatureFlags.MEMBER_BALANCE_CACHE)?.Payload.Value);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not parse variant payload for toggle {toggle}. Should be in the format {cacheSettings}. Error {ex}",
                        FeatureFlags.MEMBER_BALANCE_CACHE,
                        JsonConvert.SerializeObject(new CacheSettings()),
                        ex);
                    return new CacheSettings();
                }
            }
        }
    }
}
