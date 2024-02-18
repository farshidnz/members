using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace SettingsAPI.Common
{
    public interface IRedisUtil
    {
        Task<T> GetWithConfigurableCache<T>(string key, Func<Task<T>> cacheMissedAsync, CacheSettings cacheSettings);
    }

    public class RedisUtil : IRedisUtil
    {
        private readonly IDatabase _database;

        public RedisUtil(IDatabase database)
        {
            _database = database;
        }

        public async Task<T> GetWithConfigurableCache<T>(string key, Func<Task<T>> cacheMissedAsync, CacheSettings cacheSettings)
        {
            var timeToLive = await _database.KeyTimeToLiveAsync(key);
            if (timeToLive.HasValue && (timeToLive.Value > cacheSettings.TimeToLiveThreshold))
            {
                return await GetDataAsync(key, cacheMissedAsync, cacheSettings, timeToLive.Value - cacheSettings.TimeToLiveThreshold);
            }

            return await MissedCached(key, cacheMissedAsync, cacheSettings);
        }

        private async Task<T> GetDataAsync<T>(string key, Func<Task<T>> cacheMissedAsync, CacheSettings cacheSettings, TimeSpan ttl)
        {
            RedisValue responseFromCache = default;
            try
            {
                responseFromCache = await _database.StringGetAsync(key);

                if (responseFromCache != RedisValue.Null)
                {
                    Log.Information("Using cache for {keyName}. Cache {CacheExpiry} Max {MaxCacheExpiry}. Cache hit. Remaining TTL {ttl}", key, cacheSettings.CacheExpiry, cacheSettings.MaxCacheExpiry, ttl);
                    return JsonConvert.DeserializeObject<T>(responseFromCache);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error executing StringSetAsync in GetDataAsync: {ex}", ex);
            }

            return await MissedCached(key, cacheMissedAsync, cacheSettings);
        }

        private async Task<T> MissedCached<T>(string key, Func<Task<T>> cacheMissedAsync, CacheSettings cacheSettings)
        {
            var responseFromDb = await cacheMissedAsync();
            if (responseFromDb != null)
            {
                try
                {
                    var result = await _database.StringSetAsync(key, JsonConvert.SerializeObject(responseFromDb), cacheSettings.MaxCacheExpiry);
                }
                catch (Exception ex)
                {
                    Log.Error("Error executing StringSetAsync in MissedCached: {ex}", ex);
                }
            }

            Log.Information("Using cache for {keyName}. Cache {CacheExpiry} Max {MaxCacheExpiry}. Cache missed.", key, cacheSettings.CacheExpiry, cacheSettings.MaxCacheExpiry);
            return responseFromDb;
        }
    }

    public class CacheSettings
    {
        public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan MaxCacheExpiry { get; set; } = TimeSpan.FromHours(1);
        [JsonIgnore]
        public TimeSpan TimeToLiveThreshold => MaxCacheExpiry - CacheExpiry;
    }
}
