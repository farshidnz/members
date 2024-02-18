using System.Threading.Tasks;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Rest;

namespace SettingsAPI.Service
{
    public interface ITransactionService
    {
        Task<Paging<TransactionResult>> GetTransactions(int memberId, int limit, int offset,
            string searchText, string dateFromStr, string dateToStr, string orderBy, string sortDirection);
        Task<bool> HasApprovedPurchases(int memberId);

        Task<TotalCountResponse> GetTransactionsTotalCount(int memberId, string searchText, string dateFromStr, string dateToStr);
    }
}