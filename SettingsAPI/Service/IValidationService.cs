using System;
using System.Threading.Tasks;
using SettingsAPI.Model.Enum;

namespace SettingsAPI.Service
{
    public interface IValidationService
    {
        void ValidateQueryConditions(int limit, int offset, string dateFromStr, string dateToStr,
            string orderBy, string sortDirection, ApiUsed apiUsed);
        
        void ValidateQueryConditionsForTotalCount(string dateFromStr, string dateToStr);
        
        void ValidateEmail(string email);
        void ValidateGender(string gender);
        DateTime ValidateAndParseDateOfBirth(string dateOfBirthStr);
        void ValidatePhone(string phone);
        void ValidateOtp(string phone, string mobileOtp, string email);

        void ValidateAccountNumber(string accountNumber);
        Task ValidateBsb(string bsb);
        void ValidateAccountName(string accountName);
        void ValidatePostCode(string postCode);

        void ValidateAmount(decimal amount, decimal balance);

        void ValidatePaymentMethod(string paymentMethod);

        void ValidatePassword(string password);

        void ValidateUri(string uri);

        void ValidateEmailVerificationCode(string code, out string memberIdStr, out string hashedEmail);

        void ValidateCheckMobileLinkCode(string code);

        void ValidateName(string firstName, string lastName);

        void ValidateFeedback(string feedback);

        void ValidateAppVersion(string appVersion);

        void ValidateDeviceModel(string deviceModel);

        void ValidateOperatingSystem(string operatingSystem);
        
        void ValidateBuildNumber(string buildNumber);

        public bool IsEnumDescriptionValid<T>(string description);
    }
}