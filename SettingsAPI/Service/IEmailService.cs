using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public interface IEmailService
    {
        Task SendVerificationEmail(int memberId, string firstName, string mailTo, string hashedEmail);
        Task SendOrphanMobileEmail(int memberId, string firstName, string mailTo, string mobile);
        Task SendContactMSEmail(string firstName, string mailTo);

        Task SendEmailAfterWithdrawSuccess(string mailTo, string firstName, decimal amount, string paymentMethod);

        Task SendFeedbackToCustomerEmail(string mailTo, string firstName, string feedback);

        Task SendFeedbackToCashrewards(int memberId, string firstName, string lastName, string email, string feedback,
            string appVersion, string deviceModel, string operatingSystem, string buildNumber);
        Task SendPasswordUpdateEmail(string mailTo, string firstName);
    }
}