using Microsoft.EntityFrameworkCore;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enum = SettingsAPI.Model.Enum;

namespace SettingsAPI.Service
{
    public class PremiumMembership
    {
        public int PremiumClientId { get; set; }
        public int PremiumMemberId { get; set; }
        public bool IsCurrentlyActive { get; set; }
    }

    public interface IPremiumService
    {
        Task<PremiumMembership> GetPremiumMembership(int baseClientId, string cognitoId);

        public bool HasBearerToken { get; }
    }

    public class PremiumService : IPremiumService
    {
        private readonly ReadOnlyShopGoContext _context;

        public PremiumService(ReadOnlyShopGoContext context)
        {
            _context = context;
        }

        public async Task<PremiumMembership> GetPremiumMembership(int baseClientId, string cognitoId)
        {
            if (string.IsNullOrEmpty(cognitoId))
            {
                return null;
            }

            var premiumClientId = GetPremiumClientId(baseClientId);
            if (!premiumClientId.HasValue)
            {
                return null;
            }
          
            Person person = await GetPersonByCognitoId(cognitoId);

            if (person == null || person.PremiumStatus == (int)Enum.PremiumStatusEnum.NotEnrolled)
            {
                return null;
            }

            var premiumMember = person.CognitoMember.Select(sel => sel.Member).Where(x => x.ClientId == premiumClientId).SingleOrDefault();

            return new PremiumMembership
            {
                PremiumClientId = premiumClientId.Value,
                PremiumMemberId = premiumMember?.MemberId ?? 0,
                IsCurrentlyActive = person.PremiumStatus == (int)Enum.PremiumStatusEnum.Enrolled
            };
        }

        private Dictionary<int, int> PremiumOffers = new Dictionary<int, int>
        {
        };

        public bool HasBearerToken => throw new System.NotImplementedException();

        private int? GetPremiumClientId(int baseClientId)
        {
            return PremiumOffers.TryGetValue(baseClientId, out var premiumClientId) ? (int?)premiumClientId : null;
        }

        private async Task<Person> GetPersonByCognitoId(string cognitoId)
        {
            return await _context.Person.AsNoTracking()
                .Include(inc => inc.CognitoMember)
                .ThenInclude(inc => inc.Member)
                .Where(person => string.Equals(person.CognitoId.ToString(), cognitoId))
                .FirstOrDefaultAsync();
        }
    }
}