namespace SettingsAPI.Service
{
    public interface IMobileOptService
    {
        bool VerifyMobileOtp(string phone, string otp, string email);
        void SendMobileOtp(string phone, string email);
    }
}