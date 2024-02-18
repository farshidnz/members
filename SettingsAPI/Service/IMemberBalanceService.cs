using System.Collections.Generic;
using System.Threading.Tasks;
using SettingsAPI.EF;

namespace SettingsAPI.Service
{
    public interface IMemberBalanceService
    {
        Task<IList<MemberBalanceView>> GetBalanceViews(int[] memberIds, bool useCache);
    }
}