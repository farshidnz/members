using System.Threading.Tasks;
using SettingsAPI.Model.Rest.CreateTicket;

namespace SettingsAPI.Service
{
    public interface IFreshdeskService
    {
        Task<bool> CreateTicket(int memberId, int? personId, CreateTicketRequest ticketRequest);
    }
}