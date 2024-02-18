using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using SettingsAPI.Common;
using SettingsAPI.Model.Enum;
using Twilio;
using Twilio.Rest.Verify.V2.Service;
using System.Text.RegularExpressions;
using SettingsAPI.Service.Interface;

namespace SettingsAPI.Service
{
    public class MobileOtpService : IMobileOptService
    {
        private readonly IOptions<Settings> _settings;
        private readonly ISet<string> _whitelist;
        private static bool _skipOtp;
        private readonly IFeatureToggleService _featureToggleService;

        public MobileOtpService(IOptions<Settings> settings, IFeatureToggleService featureToggleService)
        {
            _settings = settings;
            _featureToggleService = featureToggleService;
            TwilioClient.Init(_settings.Value.AccountSId, _settings.Value.AuthToken);
            var phoneWhiteListStr = _settings.Value.OtpWhitelist;

            if (!string.IsNullOrWhiteSpace(phoneWhiteListStr))
            {
                var phoneWhiteList = phoneWhiteListStr.Split(",").Select(p => p.Trim()).ToList();;
                _whitelist = new HashSet<string>(phoneWhiteList);
            }
            else
            {
                _whitelist = new HashSet<string>();
            }

            if (_settings.Value.Devops4Enabled)
            {
                _skipOtp = _settings.Value.SkipOtp;
            }
            else
            {
                var env = System.Environment.GetEnvironmentVariable("APP_ENV");
                _skipOtp = (env == "dev");
            }
            
        }

        public bool VerifyMobileOtp(string phone, string otp, string email)
        { 
            CheckingSkipOtp(email, phone);
            if (_whitelist!.Contains(phone) ||(_skipOtp && "123456".Equals(otp)))
                return true;
            try
            {
                var verificationCheck = VerificationCheckResource.Create(
                    to: Util.ConvertPhoneToInternationFormat(phone),
                    code: otp,
                    pathServiceSid: _settings.Value.PathServiceSId
                );

                return verificationCheck.Status == "approved";
            }
            catch
            {
                return false;
            } 
        }

        public void SendMobileOtp(string phone, string email)
        {
            CheckingSkipOtp(email, phone);
            if (_whitelist!.Contains(phone) || _skipOtp)
                return;
            VerificationResource.Create(
                to: Util.ConvertPhoneToInternationFormat(phone),
                channel: ChannelSendingOtp.Sms.ToString().ToLower(),
                pathServiceSid: _settings.Value.PathServiceSId
            );
        }

        public void CheckingSkipOtp(string email, string phone)
        {
            if (_featureToggleService.IsEnable(FeatureFlags.ENABLE_OTP_BYPASS_FOR_AUTOMATION_TEST))
            { // Keep consistent behavior to bypass all OTP checking with code "123456" in development environement
                _skipOtp = _skipOtp || IsAutomationTestAccount(email, phone);
            }
        }

        /// <summary>
        /// Check whether a Member is an automation test account via the specified email and PhoneNumber
        /// </summary>
        /// <param name="email">The email.</param>
        /// <param name="phoneNumber">The phone number.</param>
        /// <returns>
        ///  no return.
        /// </returns>
        public bool IsAutomationTestAccount(string email, string phoneNumber)
        {
            Regex regEmail = new Regex(Constant.RegexEmailWhiteList);
            Regex regPhone = new Regex(Constant.RegexMobileBypassList);
            bool isMatch = false;
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(phoneNumber))
            {  // only both email and phone number meet the rule can bypass the otp checking
                isMatch = regEmail.IsMatch(email.ToLower()) && regPhone.IsMatch(phoneNumber);
            }
            return isMatch;
        }
    }
}