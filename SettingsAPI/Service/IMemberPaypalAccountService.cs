using System.Threading.Tasks;
using SettingsAPI.EF;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Rest;

namespace SettingsAPI.Service
{
    public interface IMemberPaypalAccountService
    {
        Task<MemberPaypalAccount> GetActiveMemberPaypalAccount(int memberId);

        Task UpdateMemberPaypalAccount(int memberId, string paypalEmail, string accessToken, string refreshToken,
            bool verifiedAccount);

        Task LinkMemberPaypalAccount(int memberId, string code);

        Task<LinkedPaypalAccountInfo> GetLinkedPaypalAccount(int memberId);

        Task UnlinkMemberPaypalAccount(int memberId);

       PaypalConnectUrlInfo GetPaypalConnectUrl(string redirectUri, string state);
    }
}