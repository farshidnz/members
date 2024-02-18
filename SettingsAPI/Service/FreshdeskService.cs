using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Model.Rest.CreateTicket;

namespace SettingsAPI.Service
{
    public class FreshdeskService : IFreshdeskService
    {
        private readonly IOptions<Settings> _options;
        private readonly IFreshdeskTicketHelperService _freshdeskTicketHelperService;
        private readonly ShopGoContext _context;

        public FreshdeskService(IOptions<Settings> options, 
            IFreshdeskTicketHelperService freshdeskTicketHelperService,
            ShopGoContext context)
        {
            _options = options;
            _freshdeskTicketHelperService = freshdeskTicketHelperService;
            _context = context;
        }

        public async Task<bool> CreateTicket(int memberId, int? personId, CreateTicketRequest ticketRequest)
        {

            var freshdeskApiKey = _options.Value.FreshdeskApiKey;
            var freshdeskDomain = _options.Value.FreshdeskDomain;

            var premiumStatus = 0;
            if (personId != null)
            {
                var person = await GetPerson((int)personId);
                premiumStatus = person.PremiumStatus;
            }

            return await _freshdeskTicketHelperService.CreateFreshDeskTicket(freshdeskApiKey, freshdeskDomain, memberId,
                ticketRequest, premiumStatus);
        }

        private async Task<Person> GetPerson(int personId)
        {
            return await _context.Person
                .Where(person => person.PersonId == personId)
                .FirstOrDefaultAsync();
        }
    }
}