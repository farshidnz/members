using System.Threading.Tasks;
using SettingsAPI.Model.Rest.CreateTicket;

namespace SettingsAPI.Service
{
    public interface IFreshdeskTicketHelperService
    {
        Task<bool> CreateFreshDeskTicket(string freshdeskApiKey, string freshdeskDomain, int memberId,
            CreateTicketRequest ticketRequest, int premiumStatus);
    }
}