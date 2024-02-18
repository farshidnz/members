using System.Threading.Tasks;
using SettingsAPI.EF;
using SettingsAPI.Model.Dto;

namespace SettingsAPI.Service
{
    public interface IMemberBankAccountService
    {
        Task SaveBankAccount(int memberId, string accountName, string bsb, string accountNumber, string mobileOtp);

        Task<MemberBankAccountInfo> GetBankAccountMasked(int memberId);

        Task<MemberBankAccount> GetBankAccount(int memberId);

        Task DisconnectBankAccount(int memberId);
    }
}