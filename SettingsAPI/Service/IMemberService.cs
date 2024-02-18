using System;
using System.Threading.Tasks;
using SettingsAPI.EF;
using SettingsAPI.Model;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;

namespace SettingsAPI.Service
{
    public interface IMemberService
    {
        Task UpdateDetails(int? personId, int memberId, string mobileOtp, string dob, string gender, string firstName, string lastName, string postCode);

        Task UpdateMobileNumber(MemberModel model);

        Task UpdateEmail(MemberModel model);

        Task<MemberDetails> GetMember(int? personId, int memberId);

        Task<CognitoMember> GetCashrewardsCognitoMember(string cognitoId);

        Task UpdateCommsPreferences(UpdateCommsPreferencesModel request);

        Task<MemberCommsPreferencesInfo> GetCommsPreferences(int memberId);

        Task CommsPromptShown(CommsPromptShownModel model);

        Task<string> SendMemberMobileOtp(int memberId);

        Task ChangePassword(int? personId, int memberId, string newPassword,
            string mobileOtp);

        Task CloseMemberAccount(CloseMemberAccountModel model);

        Task<string> VerifyEmail(string code);

        Task<bool> SendVerificationEmail(int memberId);

        Task<bool> SendSignupAutomatedVerificationEmail(int memberId);

        Task SendUpdateMobileLink(int memberId);

        Task UpdateMobileWithCode(string code, string mobile);

        Task CheckMobileLinkWithCode(string code);

        Task UpdateInstallNotifier(InstallNotifierModel model);

        Task FeedBack(int memberId, string feedback, string appVersion, string deviceModel, string operatingSystem, string buildNumber);

        Task<MembershipDetail> GetMembershipInfo(int memberId);

        Task<string> GetMaskedMobileNumber(int memberId);

        Task<string> GetHashedSurveyEmail(int memberId);

        Task<Member> ValidateAndGetCurrentMember(int memberId);
    }
}