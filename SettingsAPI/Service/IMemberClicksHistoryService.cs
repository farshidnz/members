using System.Threading.Tasks;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Rest;

namespace SettingsAPI.Service
{
    public interface IMemberClicksHistoryService
    {
        Task<Paging<MemberClicksHistoryResult>> GetMemberClicksHistory(int memberId, int limit, int offset,
            string searchText, string dateFromStr, string dateToStr, string orderBy, string sortDirection);

        Task<TotalCountResponse> GetMemberClicksHistoryTotalCount(int memberId, string searchText, string dateFromStr,
            string dateToStr);
    }
}