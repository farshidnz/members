using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public interface IMemberRedeemService
    {
        Task Withdraw(int memberId, decimal amount, string paymentMethod, string mobileOtp);
    }
}