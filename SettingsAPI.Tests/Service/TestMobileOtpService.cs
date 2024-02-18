using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Model.Rest.CreateTicket;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using SettingsAPI.Model.Enum;
using Twilio.Rest.Verify.V2.Service;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class TestMobileOtpService
    {
        private const string WHITELIST_MOBILE = "+61 400000004";
        private const string NON_WHITELIST_MOBILE = "+61 400000014";
        private const string OTP_CODE = "123456";
        private const string FOTP_CODE = "654321";

        [Theory]
        [InlineData(true, "qa+signup@cashrewards.com", WHITELIST_MOBILE, "prod", OTP_CODE, true)]
        [InlineData(true, "qa+signup@cashrewards.com", NON_WHITELIST_MOBILE, "prod", OTP_CODE, false)]
        [InlineData(true, "qa+signup@cashrewards.com", WHITELIST_MOBILE, "prod", FOTP_CODE, true)]
        [InlineData(false, "Qa+signup@cashrewards.com", WHITELIST_MOBILE, "prod", OTP_CODE, true)]
        [InlineData(true, "Ta+signup@cashrewards.com", WHITELIST_MOBILE, "prod", OTP_CODE, false)]
        [InlineData(true, "Qa+signup@cashrewards.com", WHITELIST_MOBILE, "dev", OTP_CODE, true)]
        [InlineData(false, "qa+Signup@cashrewards.com", WHITELIST_MOBILE, "dev", OTP_CODE, true)]
        [InlineData(true, "Qa+signup@cashreward.com", WHITELIST_MOBILE, "dev", OTP_CODE, false)]
        [InlineData(true, "Qa+signup@cashreward.com", WHITELIST_MOBILE, "dev", FOTP_CODE, false)]
        public void TestSendMemberMobileOtpWithFeatureToggle(bool featureToggle, string email, string phoneNumber, string env, string otpCode, bool isAutomationTestAccount)
        {
          var featureToggleServiceMock = new Mock<IFeatureToggleService>();
          featureToggleServiceMock.Setup(p => p.IsEnable(It.IsAny<string>())).Returns(featureToggle);
          var settings = InitMockSettings(env == "dev");

          var mobileOtpService = InitMemberOtpService(settings, featureToggleServiceMock);

          var result = mobileOtpService.VerifyMobileOtp(phoneNumber, otpCode, email);

          if((OTP_CODE == otpCode && env == "dev") || (OTP_CODE == otpCode && featureToggle && isAutomationTestAccount))
          { // return true only OTP code is correct and in Dev env or automation test account in production env 
            Assert.True(result);
          } else {
            Assert.False(result);
          }
        }

        [Theory]
        [InlineData("qa+signup01@cashrewards.com", WHITELIST_MOBILE, true)]
        [InlineData("qa+Signup01@Cashrewards.com", WHITELIST_MOBILE, true)]
        [InlineData("qa+signup01@cashrewards.com", NON_WHITELIST_MOBILE, false)]
        [InlineData("Tqa+signup01@cashrewards.com", WHITELIST_MOBILE, false)]
        [InlineData("qa+signup01@cashreward.com", WHITELIST_MOBILE, false)]
        public void IsAutomationTestAccount_Should_ReturnCorrectSkipValue(string email, string phoneNumber, bool expectedSkipOtpValue)
        {
            var featureToggleService = InitFeatureToggleServiceMock();
            var settings = InitMockSettings(false);
            var mobileOtpService = InitMemberOtpService(settings, featureToggleService);

            var result = mobileOtpService.IsAutomationTestAccount(email, phoneNumber);
            Assert.True(result == expectedSkipOtpValue);
        }

        private static Mock<IOptions<Settings>> InitMockSettings(bool skipped)
        {
            var settings = new Mock<IOptions<Settings>>();
            settings.Setup(x => x.Value).Returns(new Settings { 
              OtpWhitelist = "",
              AccountSId = "test",
              AuthToken = "test",
              Devops4Enabled = true,
              SkipOtp = skipped
            });
            return settings;
        }

        private static Mock<IFeatureToggleService> InitFeatureToggleServiceMock()
        {
            return new Mock<IFeatureToggleService>();
        }

        private static MobileOtpService InitMemberOtpService(
          Mock<IOptions<Settings>> settings = null,
          Mock<IFeatureToggleService> featureToggleServiceMock = null)
        {
          var mobileOtpService = new Mock<MobileOtpService>(settings.Object, featureToggleServiceMock.Object);

          return mobileOtpService.Object;
         }
    }
}
