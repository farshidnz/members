using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Mappers;
using SettingsAPI.Model;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using StackExchange.Redis;
using Xunit;
using static SettingsAPI.Common.Constant;

namespace SettingsAPI.Tests.Service
{
    public class TestMemberService
    {
        /* Common init data */
        private const string Mobile = "+61 411111111";
        private const string SaltKey = "saltKey";
        private const int MemberId = 10000;
        private const int PersonId = 20000;
        private const int FPersonId = 20001;
        private const int ClientIdCR = 1000000;
        private const int ClientIdANZ = 1000034;
        private const int OtherMemberId = 10001;
        private const int OtherMemberId1 = 10002;
        private const int OtherMemberId2 = 10003;
        private const string MemberNewId = "34e678b4-faec-4a01-a998-97895646d308";
        private const string CurrentPasswordEncrypted = "current password encrypted salt";
        private const string HashedEmail = "dummy hashed email";
        private const string OtherFEmailVerificationCode = "other-false-dummy-email-verification-code";
        private const string EmailVerificationCode =
            "MTAwMDAwMDAwMTpmVkRwVndMWFhhMFFSd1MxTDNBOVNTMVUzTDlQNkRDaFJlTFMxVTNMOVBmM1J5N1RsRzMwa1Flc2ZUc0xnPQ";

        /* Valid data (true) */

        private const string Email = "abc@gmail.com";
        private const string QaEmail = "qa+Signupabc@cashrewards.com";
        private const string EmailDuplicate = "xyz@gmail.com";
        private const string Gender = "Male";
        private const string DateOfBirth = "1993-05-05";
        private const string Phone = "+64 222333444";
        private const string PhoneDuplicate = "64 222333555";
        private const string Otp = "654321";
        private const string AccountNumber = "123456789987654321";
        private const string Bsb = "54321";
        private const string AccountName = "dummy";
        private const string PostCode = "1234";
        private const string NewPassword = "abc123456789";

        private const long CurrentTimeStamp = 1605889227;
        private static readonly string Code = MemberId + "$" + CurrentTimeStamp + "$" + "0X2ZSUxpj2ACPDyuQ6m269S1U3L9PqAkQFAhc5XwANi6lZba4=";
        private static readonly string OtherCode1 = OtherMemberId + "$" + CurrentTimeStamp + "$" + "other-dummy-hashcode";
        private static readonly string OtherCode2 = OtherMemberId1 + "$" + CurrentTimeStamp + "$" + "other-dummy-hashcode";

        /* Invalid data (false) */

        private const int FMemberId = 10001;
        private const string FEmail = "abcgmail.com";
        private const string FGender = "abc";
        private const string FDateOfBirth = "1993-05/05";
        private const string FPhone = "0989999999";
        private const string FOtp = "123456";
        private const string FAccountNumber = "abcdefghijk";
        private const string FBsb = "12345";
        private const string FAccountName = "";
        private const string FPostCode = "abcd";
        private const string FNewPassword = "abc1234";
        private const string FEmailVerificationCode = "false-dummy-email-verification-code";

        private static readonly string ExpiredCode =
            new StringBuilder($"{MemberId}${CurrentTimeStamp - Constant.TokenTimeToLive - 1}$8BGf4C0VxKOkUOSxFNp6gBEohNLsS1L3A9SuYHoPo4S1L3A9S9M4Nac=")
                .ToString();
        private static readonly string BadCode = "test" + "$" + CurrentTimeStamp + "$" + "other-dummy-hashcode";

        private static Mock<IDatabase> _redisDb;
        private static Mock<IMobileOptService> _mobileOtpService;

        [Theory]
        [InlineData("FirstName", "")]
        [InlineData("LastName", "")]
        [InlineData("Gender", FGender)]
        [InlineData("PostCode", FPostCode)]
        [InlineData("DateOfBirth", FDateOfBirth)]
        public void MemberDetailsRequestValidator_ShouldThrowException_GivenInvlidValues(string prop, object value)
        {
            var validaor = new MemberDetailsRequestValidator(Mock.Of<IValidationService>());
            var model = CreateMemberDetailsRequest();
            model.GetType().GetProperty(prop).SetValue(model, value);
            
            var result = validaor.TestValidate(model);
            result.ShouldHaveValidationErrorFor(prop);
        }

        [Theory]
        [InlineData("FirstName", "f-name")]
        [InlineData("LastName", "l-name")]
        [InlineData("Gender", "Male")]
        [InlineData("PostCode", PostCode)]
        [InlineData("DateOfBirth", DateOfBirth)]
        public void MemberDetailsRequestValidator_ShouldValidate_GivenVlidValues(string prop, object value)
        {
            var validationService = new ValidationService(Mock.Of<IMobileOptService>(), 
                Mock.Of<IAwsService>(), 
                Mock.Of<IOptions<Settings>>(),
                Mock.Of<ITimeService>());
            var validaor = new MemberDetailsRequestValidator(validationService);
            var model = CreateMemberDetailsRequest();
            model.GetType().GetProperty(prop).SetValue(model, value);

            var result = validaor.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        private MemberDetailsRequest CreateMemberDetailsRequest()
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(new StringGenerator(() =>
                                        Guid.NewGuid().ToString().Substring(0, 10)));

            var model = fixture.Create<MemberDetailsRequest>();
            model.DateOfBirth = "2000-02-02";
            model.Gender = "Male";
            model.PostCode = "1234";
            return model;
        }

        [Fact]
        public async Task UpdateDetails_ShouldThrowExcption_GivenMemberNotfound()
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode()
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
               memberService.UpdateDetails(FPersonId+1, memberId, Otp, DateOfBirth, Gender, "abc", "xyz",
               PostCode));

            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
               memberService.UpdateDetails(null, FMemberId, Otp, DateOfBirth, Gender, "abc", "xyz",
               PostCode));

        }

        [Theory]
        [InlineData(PersonId, FMemberId)]
        [InlineData(null, MemberId)]
        [InlineData(PersonId, MemberId)]
        public async Task UpdateDetails_ShouldGetMembers_GivenPersonIdOrMemberId(int? personId, int memberId)
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            await memberService.UpdateDetails(personId, memberId, Otp, DateOfBirth, Gender, "abc", "xyz", PostCode);
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once());
        }

        [Fact]
        public async Task TestUpdateDetail()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Valid
            await memberService.UpdateDetails(PersonId, MemberId, Otp, DateOfBirth, Gender, "abc", "xyz", PostCode);
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once());
        }

        [Fact]
        public async Task TestUpdateMobileNumber()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);
            var memberModel = new MemberModel() { MemberId = MemberId, MobileOtp = FOtp, MobileNumber = Mobile, PersonId= PersonId };
           
            //Invalid otp
            await Assert.ThrowsAsync<InvalidMobileOtpException>(() => memberService.UpdateMobileNumber(memberModel));

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.UpdateMobileNumber(
                new MemberModel() { MemberId = It.IsAny<int>(), MobileNumber = Phone, MobileOtp = It.IsAny<string>(), PersonId = It.IsAny<int>() }));
            
         
            memberModel.MobileNumber = PhoneDuplicate+1;
            memberModel.MobileOtp = Otp;
            memberModel.MemberId = MemberId;
            //Phone already use by other member
            await Assert.ThrowsAsync<DuplicateMobileNumberException>(() => memberService.UpdateMobileNumber(
                memberModel));

            //Valid
            //Mobile not change
            memberModel.MobileNumber = PhoneDuplicate+1;
            memberModel.PersonId = 20001;
            await Assert.ThrowsAsync<BadRequestException>(() => memberService.UpdateMobileNumber(
            memberModel));

            memberModel.MobileNumber = Mobile;
            memberModel.PersonId = 20000;
            //Mobile change
            await memberService.UpdateMobileNumber(memberModel);
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task TestUpdateEmail()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);
            var memberModel = new MemberModel() { MemberId = MemberId, MobileOtp = FOtp, MobileNumber = Mobile, PersonId = PersonId , Email = Email};

            memberModel.PersonId = It.IsAny<int>();
            memberModel.MobileOtp = It.IsAny<string>();
            memberModel.Email = EmailDuplicate;
            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.UpdateEmail(memberModel));

            memberModel.PersonId = FMemberId;
            memberModel.MobileOtp = It.IsAny<string>();
            memberModel.Email = EmailDuplicate;
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.UpdateEmail(memberModel));

            //Invalid otp
            memberModel.PersonId = PersonId;
            memberModel.MobileOtp = FOtp;
            memberModel.Email = EmailDuplicate;
            await Assert.ThrowsAsync<InvalidMobileOtpException>(() => memberService.UpdateEmail(memberModel));

            //Email already use by other member
            memberModel.MobileOtp = Otp;
            await Assert.ThrowsAsync<DuplicateEmailException>(() => memberService.UpdateEmail(memberModel));

            //Valid
            memberModel.Email = Email;
            await memberService.UpdateEmail(memberModel);
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once());
        }
        
        [Fact]
        public async Task TestUpdateQaEmailFeatureToggleOn()
        {
            var newMember = new Member()
            {
                MemberId = 1001,
                Mobile = Phone,
                PersonId = 20001,
                Email = Email
            };
            var shopGoContext = InitShopGoContext(newMember);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var featureToggleServiceMock = InitFeatureServiceEnabledMock();
            var encryptionService = InitEncryptionServiceMock();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext, featureToggleServiceMock, encryptionService);
            var memberModel = new MemberModel() { MemberId = 1001, MobileOtp = FOtp, MobileNumber = Mobile, PersonId = 20001 , Email = QaEmail};

            await memberService.UpdateEmail(memberModel);
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once());
            var member = shopGoContext.Object.Member.FirstOrDefault(m => m.PersonId == memberModel.PersonId);
            Assert.True(member!.IsValidated);
        }
        
        [Fact]
        public async Task TestUpdateQaEmailFeatureToggleOff()
        {
            var newMember = new Member()
            {
                MemberId = 1001,
                Mobile = Phone,
                PersonId = 20001,
                Email = Email
            };
            var shopGoContext = InitShopGoContext(newMember);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var featureToggleServiceMock = InitFeatureServiceDisabledMock();
            var encryptionService = InitEncryptionServiceMock();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext, featureToggleServiceMock, encryptionService);
            var memberModel = new MemberModel() { MemberId = 1001, MobileOtp = FOtp, MobileNumber = Phone, PersonId = 20001 , Email = QaEmail};

            await memberService.UpdateEmail(memberModel);
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once());
            var member = shopGoContext.Object.Member.FirstOrDefault(m => m.PersonId == memberModel.PersonId);
            Assert.True(member!.IsValidated == false);
        }
        
        [Fact]
        public async Task TestUpdateNoneQaEmailFeatureToggleOn()
        {
            var newMember = new Member()
            {
                MemberId = 1001,
                Mobile = Phone,
                PersonId = 20001,
                Email = Email
            };
            var shopGoContext = InitShopGoContext(newMember);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var featureToggleServiceMock = InitFeatureServiceEnabledMock();
            var encryptionService = InitEncryptionServiceMock();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext, featureToggleServiceMock, encryptionService);
            var memberModel = new MemberModel() { MemberId = 1001, MobileOtp = FOtp, MobileNumber = Phone, PersonId = 20001 , Email = "test2@gmail.com" };
            
            await memberService.UpdateEmail(memberModel);
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once());
            var member = shopGoContext.Object.Member.FirstOrDefault(m => m.PersonId == memberModel.PersonId);
            Assert.True(member!.IsValidated == false);
        }

        [Fact]
        public async Task TestGetMember()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), FMemberId));

            var member = await memberService.GetMember(null, MemberId);

            member.MemberId.Should().Be(MemberId);
            member.NewMemberId.ToLower().Should().Be(MemberNewId.ToLower());
            member.Mobile.Should().Be(Mobile);
            member.ReceiveNewsLetter.Should().BeTrue();
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        public async Task GetMemberShouldReturnShowCommunicationsPromptTrue_WhenPromptShown0or1Times(int timesShown, bool expectedShowCommunicationsPrompt)
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                MemberNewId = new Guid(MemberNewId),
                SaltKey = SaltKey,
                Mobile = Mobile,
                UserPassword = CurrentPasswordEncrypted,
                HashedEmail = HashedEmail,
                Status = StatusType.Active.GetHashCode(),
                CommsPromptShownCount = timesShown
            };


            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), FMemberId));

            var memberOut = await memberService.GetMember(null, memberId);

            memberOut.ShowCommunicationsPrompt.Should().Be(expectedShowCommunicationsPrompt);
        }

        [Fact]
        public async Task GetMemberShouldReturnShowCommunicationsPromptFalse_WhenCacheValueIndicatesPromptHasBeenShownInLast24Hours()
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode(),
                CommsPromptShownCount = 0
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            _redisDb.Setup(db => db.StringGetAsync("comms_prompt_shown:1234", CommandFlags.None)).Returns(Task.FromResult(new RedisValue("true")));

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), FMemberId));

            var memberOut = await memberService.GetMember(null, memberId);

            memberOut.ShowCommunicationsPrompt.Should().BeFalse();
        }

        [Fact]
        public async Task GetMemberShouldReturnShowCommunicationsPromptFalse_WhenUserCreatedSinceNewCommunicationsOptionsAdded()
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode(),
                CommsPromptShownCount = 0,
                DateJoined = new DateTime(2021, 5, 31)
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMember(It.IsAny<int>(), FMemberId));

            var memberOut = await memberService.GetMember(null, memberId);
            memberOut.ShowCommunicationsPrompt.Should().BeFalse();
            member.DateJoined = new DateTime(2021, 5, 29);
            memberOut = await memberService.GetMember(null, memberId);
            memberOut.ShowCommunicationsPrompt.Should().BeTrue();
        }

        [Theory]
        [InlineData(Constant.PremiumStatus.NotEnrolled, false)]
        [InlineData(Constant.PremiumStatus.Enrolled, true)]
        [InlineData(Constant.PremiumStatus.OptOut, false)]
        public async Task GetMember_ShouldReturnIsPremiumFlag_GivenPremiumStatus(int premiumStatus, bool expectedIsPremium)
        {
            var personId = 123456;
            var person = new Person
            {
                PersonId = personId,
                PremiumStatus = premiumStatus
            };

            var shopGoContext = InitShopGoContext(null, person);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(null, person);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            var memberOut = await memberService.GetMember(personId, MemberId);

            memberOut.IsPremium.Should().Be(expectedIsPremium);
        }


        [Theory]
        [InlineData(Constant.PremiumStatus.NotEnrolled)]
        [InlineData(Constant.PremiumStatus.Enrolled)]
        [InlineData(Constant.PremiumStatus.OptOut)]
        public async Task GetMember_ShouldReturnPremiumStatus(int premiumStatus)
        {
            var personId = 123456;
            var person = new Person
            {
                PersonId = personId,
                PremiumStatus = premiumStatus
            };

            var shopGoContext = InitShopGoContext(null, person);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(null, person);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            var memberOut = await memberService.GetMember(personId, MemberId);

            memberOut.PremiumStatus.Should().Be(premiumStatus);
        }

        [Fact]
        public async Task GetMember_ShouldReturnPremiumStatusOfZero_WhenPersonIsNull()
        {
            var personId = 123456;
            Person person = null;

            var shopGoContext = InitShopGoContext(null, person);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            var memberOut = await memberService.GetMember(personId, MemberId);

            memberOut.PremiumStatus.Should().Be(Constant.PremiumStatus.NotEnrolled);
        }

        [Theory]
        [InlineData(112233)]
        [InlineData(null)]
        public async Task GetMember_ShouldReturnIsPremiumFlagAsFalse_GivenPersonDoesNotExist(int? personId)
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            var memberOut = await memberService.GetMember(personId, MemberId);

            memberOut.IsPremium.Should().BeFalse();
        }

        [Theory]
        [InlineData(null,FMemberId)]
        [InlineData(FPersonId, MemberId)]
        public async Task MyTestMethod(int? personId, int memberId )
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);
            var model = new UpdateCommsPreferencesModel
            {
                PersonId = personId,
                MemberId = memberId
            };
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.UpdateCommsPreferences(model));
        }

        [Fact]
        public async Task TestUpdateCommsPreferences()
        {
            var memberId = 1234;
            var member = new Member
            {
                PersonId = PersonId,
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode()
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            await memberService.UpdateCommsPreferences(new UpdateCommsPreferencesModel
            {
                PersonId = PersonId,
                MemberId = memberId,
                SubscribeNewsletters = true,
                SubscribeSMS = false
            });
            member.ReceiveNewsLetter.Should().BeTrue();
            member.SmsConsent.Should().BeFalse();
            member.AppNotificationConsent.Should().BeNull();
            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task UpdateCommsPreferences_ShouldPreventCommsPopupFromAppearing()
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode(),
                CommsPromptShownCount = 0
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Valid
            await memberService.UpdateCommsPreferences(new UpdateCommsPreferencesModel
            {
                PersonId = null,
                MemberId = memberId,
                SubscribeNewsletters = true,
            });
            member.CommsPromptShownCount.Should().Be(Constant.MaxCommsPromptShownCount);

            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Exactly(1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateCommsPreferences_ShouldNotOverwriteSMSAndAppNotificationsValues_WhenTheyAreNullInRequest(bool savedValue)
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode(),
                SmsConsent = savedValue,
                AppNotificationConsent = savedValue
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            await memberService.UpdateCommsPreferences(new UpdateCommsPreferencesModel
            {
                PersonId = null,
                MemberId = memberId,
                SubscribeNewsletters = true
            });
            member.SmsConsent.Should().Be(savedValue);
            member.AppNotificationConsent.Should().Be(savedValue);

            shopGoContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once);
        }


        [Fact]
        public async Task TestGetMemberCommsPreferences()
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                MemberNewId = new Guid(MemberNewId),
                SaltKey = SaltKey,
                Mobile = Mobile,
                UserPassword = CurrentPasswordEncrypted,
                HashedEmail = HashedEmail,
                Status = StatusType.Active.GetHashCode(),
                SmsConsent = true,
                ReceiveNewsLetter = true,
                AppNotificationConsent = true
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.GetCommsPreferences(It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.GetCommsPreferences(FMemberId));

            try
            {
                var result = await memberService.GetCommsPreferences(memberId);
                result.SubscribeNewsletters.Should().BeTrue();
                result.SubscribeSMS.Should().BeTrue();
                result.SubscribeAppNotifications.Should().BeTrue();

            }
            catch (MemberNotFoundException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Theory]
        [InlineData(0, CommsPromptDismissalAction.Close, 1)]
        [InlineData(1, CommsPromptDismissalAction.Close, 2)]
        [InlineData(0, CommsPromptDismissalAction.Review, 2)]
        [InlineData(1, CommsPromptDismissalAction.Review, 2)]
        public async Task TestCommsPromptShownWithAction(int initialCommsPromptShownCount,CommsPromptDismissalAction action, int expectedCommsPromptShownCount)
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode(),
                CommsPromptShownCount = initialCommsPromptShownCount
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            var modelMemberNotFound = new CommsPromptShownModel
            {
                MemberId = FMemberId,
                Action = action
            };
            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.CommsPromptShown(modelMemberNotFound));
            var model = new CommsPromptShownModel
            {
                MemberId = memberId,
                Action = action
            };
            await memberService.CommsPromptShown(model);

            member.CommsPromptShownCount.Should().Be(expectedCommsPromptShownCount);

            shopGoContext.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);

        }


        [Fact]
        public async Task CommsPromptShown_WithFirstCloseAction_ShouldSetCacheValueToNotShowAgainFor24Hours()
        {
            var memberId = 1234;
            var member = new Member
            {
                MemberId = memberId,
                Status = StatusType.Active.GetHashCode(),
                CommsPromptShownCount = 0
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            var modelMemberNotFound = new CommsPromptShownModel
            {
                MemberId = FMemberId,
                Action = It.IsAny<CommsPromptDismissalAction>()
            };
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.CommsPromptShown(modelMemberNotFound));
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.GetCommsPreferences(FMemberId));

            var model = new CommsPromptShownModel
            {
                MemberId = memberId,
                Action = CommsPromptDismissalAction.Close
            };
            await memberService.CommsPromptShown(model);

            _redisDb.Verify(db => db.StringSetAsync("comms_prompt_shown:1234", "true", TimeSpan.FromHours(24), When.Always, CommandFlags.None));

            shopGoContext.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task TestSendMemberMobileOtp()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.SendMemberMobileOtp(It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.SendMemberMobileOtp(FMemberId));
            try
            {
                await memberService.SendMemberMobileOtp(MemberId);
            }
            catch (MemberNotFoundException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Theory]
        [InlineData(PersonId, MemberId, FOtp)]
        [InlineData(PersonId, FMemberId, FOtp)]
        [InlineData(null, MemberId, FOtp)]
        public async Task ChangePassword_ShouldThrowException_GivenInvalidOtp(int? personId, int memberId, string otp)
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            await Assert.ThrowsAsync<InvalidMobileOtpException>(() =>
                memberService.ChangePassword(personId, memberId, NewPassword, otp));
        }

        [Theory]
        [InlineData(PersonId, MemberId)]
        [InlineData(null, MemberId)]
        public async Task ChangePassword_ShouldValidatOtp_GivenValidOtp(int? personId, int memberId)
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            await memberService.ChangePassword(personId, memberId, NewPassword, Otp);
            shopGoContext.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once());
        }

        [Fact]
        public async Task ChangePassword_ShouldThrowException_GivenInvalidMemberId()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.ChangePassword(null, FMemberId, NewPassword, Otp));
        }

        [Fact]
        public async Task ChangePassword_ShoudSaveChanges_GivenValidData()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            await memberService.ChangePassword(PersonId,MemberId, NewPassword, Otp);
            shopGoContextMock.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);

        }


        [Theory]
        [InlineData(null, FMemberId)]
        [InlineData(FPersonId, FMemberId)]
        [InlineData(PersonId, FMemberId)]
        public async Task TestCloseApi_ShouldThrowMemberNotFoundException_GivenInvlidData(int? personId, int memberId)
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            //Member not found 
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                    memberService.CloseMemberAccount(new CloseMemberAccountModel { PersonId = personId, MemberId = memberId }));

        }

        [Theory]
        [InlineData(PersonId, MemberId)]
        [InlineData(null, MemberId)]
        public async Task TestCloseApi_ShouldNotThrowException_GivenValidData(int? personId, int memberId )
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            await memberService.CloseMemberAccount(new CloseMemberAccountModel {PersonId = personId, MemberId = memberId});
            shopGoContextMock.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);

            shopGoContextMock.Object.Member.FirstOrDefault(m => m.MemberId == memberId).Status
                .Should().Be(StatusType.Deleted.GetHashCode());
        }

        [Fact]
        public async Task TestVerifyEmail()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            /* Invalid */

            await Assert.ThrowsAsync<InvalidEmailVerificationCodeException>(() =>
                memberService.VerifyEmail(FEmailVerificationCode));

            await Assert.ThrowsAsync<InvalidEmailVerificationCodeException>(() =>
                memberService.VerifyEmail(OtherFEmailVerificationCode));

            /* Valid */
            await memberService.VerifyEmail(EmailVerificationCode);
            shopGoContextMock.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);
        }
        [Fact]
        public async Task TestSendVerificationEmail()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            //Member not found 
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.SendVerificationEmail(It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.SendVerificationEmail(FMemberId));

            shopGoContextMock.Setup(s => s.Member).ReturnsDbSet(new[]
            {
                new Member
                {
                    MemberId = OtherMemberId, IsValidated = false, Status = StatusType.Active.GetHashCode(), Email = "dummy@fd.com", HashedEmail = "fdfsdfdsfds"
                },
                new Member
                {
                    MemberId = MemberId, IsValidated = true, Status = StatusType.Active.GetHashCode()
                }
            });
            //Email is sent
            Assert.True(await memberService.SendVerificationEmail(OtherMemberId));

            //Email is not send
            Assert.False(await memberService.SendVerificationEmail(MemberId));
        }

        [Theory]
        [InlineData(SignupAutomatedVerificationEmailStatus.NotSent)]
        [InlineData(SignupAutomatedVerificationEmailStatus.Sending)]
        [InlineData(SignupAutomatedVerificationEmailStatus.Sent)]
        public async Task WhenSendingAutomatedSignupVerificationEmail_ShouldNotSendDuplicatesForMemeber(SignupAutomatedVerificationEmailStatus currentStatus)
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            shopGoContextMock.Setup(s => s.Member).ReturnsDbSet(new[]
            {
                new Member
                {
                    MemberId = MemberId, IsValidated = false, Status = (int)StatusType.Active,
                    SignupVerificationEmailSentStatus = (int)currentStatus,
                    Email = "dummy@fd.com", HashedEmail = "fdfsdfdsfds"
                }
            });

            switch (currentStatus)
            {
                case SignupAutomatedVerificationEmailStatus.NotSent:
                    //Email is sent for first time
                    Assert.True(await memberService.SendSignupAutomatedVerificationEmail(MemberId));
                    break;

                case SignupAutomatedVerificationEmailStatus.Sending:
                case SignupAutomatedVerificationEmailStatus.Sent:

                    //Email is not sent, as its busy sending or already sent
                    Assert.False(await memberService.SendSignupAutomatedVerificationEmail(MemberId));
                    break;

                default:
                    break;
            }
        }

        [Fact]
        public async Task TestSendUpdateMobileLink()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.SendUpdateMobileLink(It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.SendUpdateMobileLink(FMemberId));

            //Send
            try
            {
                await memberService.SendUpdateMobileLink(MemberId);
            }
            catch (MemberNotFoundException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public async Task TestUpdateMobileWithCode()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            //Token expired
            await Assert.ThrowsAsync<TokenExpiredException>(() =>
                memberService.UpdateMobileWithCode(ExpiredCode, It.IsAny<string>()));
            
            //Invalid mobile
            await Assert.ThrowsAsync<InvalidMobileNumberException>(() =>
                memberService.UpdateMobileWithCode(OtherCode2, FPhone));
            
            //Member not found 
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.UpdateMobileWithCode(OtherCode2, PhoneDuplicate));
            
            //Unauthorized
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.UpdateMobileWithCode(OtherCode1, PhoneDuplicate));

            //Mobile already use by other member
            await Assert.ThrowsAsync<DuplicateMobileNumberException>(() =>
                memberService.UpdateMobileWithCode(Code, PhoneDuplicate));

            //Valid
            await memberService.UpdateMobileWithCode(Code, Phone);
            shopGoContextMock.Verify(s=>s.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task CheckMobileLinkWithCode()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            //Token expired
            await Assert.ThrowsAsync<BadRequestException>(() =>
                memberService.CheckMobileLinkWithCode(BadCode));

            //Token expired
            await Assert.ThrowsAsync<TokenExpiredException>(() =>
                memberService.CheckMobileLinkWithCode(ExpiredCode));

            //Member not found 
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.CheckMobileLinkWithCode(OtherCode2));

            //Unauthorized
            await Assert.ThrowsAsync<MemberNotFoundException>(() =>
                memberService.CheckMobileLinkWithCode(OtherCode1));

            //Valid
            try
            {
                await memberService.CheckMobileLinkWithCode(Code);
            }
            
            catch (Exception ex)
            {
                Assert.True(ex == null);
            }
        }

        [Theory]
        [InlineData(PersonId, MemberId)]
        [InlineData(null, MemberId)]
        public async Task TestUpdateInstallNotifier(int? personId, int memberId)
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            var installNotifierModel = new InstallNotifierModel
            {
                PersonId = personId,
                MemberId = memberId
            };

            //Valid
            installNotifierModel.Status = true;
            await memberService.UpdateInstallNotifier(installNotifierModel);
            installNotifierModel.Status = false;
            await memberService.UpdateInstallNotifier(installNotifierModel);
            shopGoContextMock.Verify(s=>s.SaveChangesAsync(CancellationToken.None), Times.Exactly(2));
        }

        [Fact]
        public async Task TestFeedback()
        {
            var shopGoContextMock = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContext);

            //Feedback invalid
            await Assert.ThrowsAsync<InvalidFeedbackException>(() => memberService.FeedBack(
                It.IsAny<int>(), null, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()));

            //App version invalid
            await Assert.ThrowsAsync<InvalidAppVersionException>(() => memberService.FeedBack(
                It.IsAny<int>(), "abc", null, It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()));

            //Device model invalid
            await Assert.ThrowsAsync<InvalidDeviceModelException>(() => memberService.FeedBack(
                It.IsAny<int>(), "abc", "abc", null,
                It.IsAny<string>(), It.IsAny<string>()));

            //Invalid operating system
            await Assert.ThrowsAsync<InvalidOperationException>(() => memberService.FeedBack(
                It.IsAny<int>(), "abc", "abc", "abc",
                null, It.IsAny<string>()));

            //Invalid build number
            await Assert.ThrowsAsync<InvalidBuildNumberException>(() => memberService.FeedBack(
                It.IsAny<int>(), "abc", "abc", "abc",
                "abc", null));

            //Valid
            try
            {
                await memberService.FeedBack(MemberId, "abc", "abc", "abc",
                    "abc", "abc");
            }
            catch (Exception ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public async Task GetMembershipInfoShouldIncludeMemberIdClientIdAndPersonId()
        {
            var theMemberId = 12345;
            var theClientId = Clients.CashRewards;
            var thePersonId = 98765;
            var theCognitoId = Guid.NewGuid();

            var theMember = new Member
            {
                ClientId = theClientId,
                MemberId = theMemberId,
                MemberNewId = new Guid(MemberNewId),
                SaltKey = SaltKey,
                Mobile = Mobile,
                UserPassword = CurrentPasswordEncrypted,
                HashedEmail = HashedEmail,
                Status = StatusType.Active.GetHashCode(),
                ReceiveNewsLetter = true,
                SmsConsent = true
            };

            var thePerson = new Person()
            {
                CognitoId = theCognitoId,
                PersonId = thePersonId,
                PremiumStatus = (int)PremiumStatusEnum.Enrolled
            };

            var theCognitoMember = new CognitoMember()
            {
                PersonId = thePersonId,
                CognitoId = theCognitoId.ToString(),
                MemberId = theMemberId
            };

            var shopGoContextMock = InitShopGoContext(theMember, thePerson, theCognitoMember);
            var readOnlyShopGoContextMock = InitReadOnlyShopGoContext(theMember, thePerson, theCognitoMember);
            var memberService = InitMemberService(shopGoContextMock, readOnlyShopGoContextMock);

            var result = await memberService.GetMembershipInfo(theMemberId);

            result.Items.Count.Should().Be(1);

            result.Items[0].MemberId.Should().Be(theMemberId);
            result.Items[0].ClientId.Should().Be(theClientId);

            result.Items[0].PersonId.Should().NotBeNull();
            result.Items[0].PersonId.Should().Be(thePersonId);

            result.Items[0].PremiumStatus.Should().Be(1);
        }

        [Fact]
        public async Task TestGetMaskedMobile()
        {
            var NoMobileMemberId = 1234;
            var member = new Member
            {
                MemberId = NoMobileMemberId,
                Status = StatusType.Active.GetHashCode(),
                Mobile = null,
                PersonId = 2003,
                ClientId = ClientIdCR
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMaskedMobileNumber(It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetMaskedMobileNumber(FMemberId));
            
            var maskedMobile = await memberService.GetMaskedMobileNumber(MemberId);
            Assert.Contains("*", maskedMobile);

            maskedMobile = await memberService.GetMaskedMobileNumber(NoMobileMemberId);
            Assert.Null(maskedMobile);
        }

        [Fact]
        public async Task TestGetHashedEmail()
        {
            var MemberIdNoEmail = 1234;
            var member = new Member
            {
                MemberId = MemberIdNoEmail,
                Status = StatusType.Active.GetHashCode(),
                Email = null,
                PersonId = 2003,
                ClientId = ClientIdCR
            };

            var shopGoContext = InitShopGoContext(member);
            var readOnlyShopGoContext = InitReadOnlyShopGoContext(member);
            var memberService = InitMemberService(shopGoContext, readOnlyShopGoContext);

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetHashedSurveyEmail(It.IsAny<int>()));
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberService.GetHashedSurveyEmail(FMemberId));

            var hashedEmail = await memberService.GetHashedSurveyEmail(MemberId);
            Assert.True(hashedEmail.Length == 64);
            Assert.Equal("028f015e6ab7471ba6786ce19107af63b16d6455e062105cff797f51d0b2c1f3", hashedEmail);

            try
            {
                await memberService.GetHashedSurveyEmail(MemberIdNoEmail);
            }
            catch (Exception ex)
            {
                Assert.True(ex.Message == "INVALID or NULL MemberEmail or AskNicelySecret");
            }
        }

        private static MemberService InitMemberService(IMock<ShopGoContext> shopGoContextMock,
            IMock<ReadOnlyShopGoContext> readOnlyShopGoContextMock,
            Mock<IFeatureToggleService> featureToggleServiceMock = null,
            Mock<IEncryptionService> encryptionServiceMock = null)
        {
            var validationService = InitValidationService();

            _mobileOtpService = InitMobileOtpServiceMock();

            var memberBalanceServiceMock = InitMemberBalanceServiceMock();

            var emailService = InitEmailServiceMock();

            var timeService = InitTimeServiceMock();

            var awsService = InitAwsServiceMock();

            _redisDb = InitRedisDbMock();

            var settings = InitMockSettings();

            var mapper = InitMapper();

            featureToggleServiceMock ??= InitFeatureServiceDisabledMock();

            var memberService = new Mock<MemberService>(settings.Object,
                shopGoContextMock.Object,
                readOnlyShopGoContextMock.Object,
                encryptionServiceMock != null ? encryptionServiceMock!.Object : new EncryptionService(),
                memberBalanceServiceMock.Object,
                _mobileOtpService.Object,
                validationService.Object,
                emailService.Object, 
                timeService.Object,
                awsService.Object,
                _redisDb.Object,
                mapper,
                Mock.Of<IEntityAuditService>(),
                Mock.Of<IFieldAuditService>(),
                featureToggleServiceMock.Object);

            memberService.CallBase = true;
            memberService.Setup(s => s.GetMemberWelcomeBonus(It.IsAny<int>())).Returns(new List<WelcomeBonusTransaction>());

            return memberService.Object;
        }

        private static Mock<IEncryptionService> InitEncryptionServiceMock()
        {
            var encryptionService = new Mock<IEncryptionService>();
            encryptionService.Setup(t => t.EncryptWithSalt(It.IsAny<string>(), It.IsAny<string>())).Returns("+somehash/");
            return encryptionService;
        }

        private static IMapper InitMapper()
        {
            var config = new MapperConfiguration(c => c.AddProfile<MemberProfile>());
            var mapper = config.CreateMapper();
            return mapper;
        }

        private static Mock<IOptions<Settings>> InitMockSettings()
        {
            var settings = new Mock<IOptions<Settings>>();
            settings.Setup(x => x.Value).Returns(new Settings { SaltKey = "ABCDEFG", AskNicelySecret = "XYZ"});
            return settings;
        }

        private static Mock<ShopGoContext> InitShopGoContext(Member member = null, Person person = null, CognitoMember cognitoMember = null)
        {

            var members = InitMembers();

            if (member != null)
            {
                members.Add(member);
            }

            var memberBalanceViews = InitMemberBalanceViews();

            var cognitoMembers = InitCognitoMembers();


            if (cognitoMember != null)
            {
                cognitoMembers.Add(cognitoMember);
            }

            var people = InitPeople();

            if (person != null)
            {
                people.Add(person);
            }

            var optionsBuilder = new DbContextOptionsBuilder<ShopGoContext>();

            var contextMock = new Mock<ShopGoContext>(optionsBuilder.Options);
            contextMock.Setup(p => p.Member).ReturnsDbSet(members);
            contextMock.Setup(p => p.MemberBalanceView).ReturnsDbSet(memberBalanceViews);
            contextMock.Setup(p => p.CognitoMember).ReturnsDbSet(cognitoMembers);
            contextMock.Setup(p => p.Person).ReturnsDbSet(people);

            var data = members.AsQueryable();
            //var mockSet = new Mock<DbSet<Member>>();
            contextMock.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(data.Provider);
            contextMock.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(data.Expression);
            contextMock.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(data.ElementType);
            contextMock.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            
            return contextMock;
        }

        private static Mock<ReadOnlyShopGoContext> InitReadOnlyShopGoContext(Member member = null, Person person = null, CognitoMember cognitoMember = null)
        {

            var members = InitMembers();

            if (member != null)
            {
                members.Add(member);
            }

            var memberBalanceViews = InitMemberBalanceViews();

            var cognitoMembers = InitCognitoMembers();


            if (cognitoMember != null)
            {
                cognitoMembers.Add(cognitoMember);
            }

            var people = InitPeople();

            if (person != null)
            {
                people.Add(person);
            }

            var optionsBuilder = new DbContextOptionsBuilder<ReadOnlyShopGoContext>();

            var contextMock = new Mock<ReadOnlyShopGoContext>(optionsBuilder.Options);
            contextMock.Setup(p => p.Member).ReturnsDbSet(members);
            contextMock.Setup(p => p.MemberBalanceView).ReturnsDbSet(memberBalanceViews);
            contextMock.Setup(p => p.CognitoMember).ReturnsDbSet(cognitoMembers);
            contextMock.Setup(p => p.Person).ReturnsDbSet(people);

            var data = members.AsQueryable();
            //var mockSet = new Mock<DbSet<Member>>();
            contextMock.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(data.Provider);
            contextMock.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(data.Expression);
            contextMock.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(data.ElementType);
            contextMock.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            return contextMock;
        }

        private static List<Person> InitPeople()
        {
            return new List<Person>
            {
                new Person
                {
                    CognitoId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    PersonId = 100000,
                    PremiumStatus = 0
                }
            };
        }

        private static List<CognitoMember> InitCognitoMembers()
        {
            return new List<CognitoMember>
            {
                new CognitoMember
                {
                    MemberId = MemberId,
                    CognitoId = "11111111-1111-1111-1111-111111111111"
                }
            };
        }

        private static List<MemberBalanceView> InitMemberBalanceViews()
        {
            return new List<MemberBalanceView>
            {
                new MemberBalanceView
                {
                    MemberID = MemberId
                }
            };
        }

        private static List<Member> InitMembers()
        {
            return new List<Member>
            {
                new Member
                {
                    PersonId = PersonId, MemberId = MemberId, MemberNewId = new Guid(MemberNewId), SaltKey = SaltKey, Mobile = Mobile,
                    UserPassword = CurrentPasswordEncrypted, HashedEmail = HashedEmail,
                    Status = StatusType.Active.GetHashCode(), ReceiveNewsLetter = true, SmsConsent = true,
                    ClientId= ClientIdCR, Email = Email
                },
                new Member
                {
                    PersonId = PersonId, MemberId = OtherMemberId2,  Mobile = PhoneDuplicate, Email = EmailDuplicate, Status = StatusType.NotAssigned.GetHashCode(),
                    ClientId = ClientIdANZ
                },
                new Member
                {
                    PersonId = 20001, MemberId = 10004,  Mobile = PhoneDuplicate+1, Email = EmailDuplicate, Status = StatusType.NotAssigned.GetHashCode(),
                    ClientId = ClientIdANZ
                },
                 new Member
                {
                    PersonId = 20001, MemberId = 10005,  Mobile = PhoneDuplicate+1, Email = EmailDuplicate, Status = StatusType.NotAssigned.GetHashCode(),
                    ClientId = ClientIdCR
                }
            };
        }

        private static Mock<IValidationService> InitValidationService()
        {
            var validationService = new Mock<IValidationService>();
            
            /* Invalid email */
            validationService.Setup(v => v.ValidateEmail(FEmail)).Throws(new InvalidEmailException());
            /* Valid email */
            validationService.Setup(v => v.ValidateEmail(Email));
            validationService.Setup(v => v.ValidateEmail(EmailDuplicate));
            
            /* Invalid gender */
            validationService.Setup(v => v.ValidateGender(FGender)).Throws(new InvalidGenderException());
            /* Valid gender */
            validationService.Setup(v => v.ValidateGender(Gender));

            /* Invalid date of birth */
            validationService.Setup(v => v.ValidateAndParseDateOfBirth(FDateOfBirth))
                .Throws(new InvalidDateOfBirthException(string.Format(AppMessage.FieldInvalid, "Date of birth")));
            /* Valid date of birth*/
            validationService.Setup(v => v.ValidateAndParseDateOfBirth(DateOfBirth)).Returns(
                DateTime.ParseExact("1993-05-05", Constant.DateOfBirthFormat, CultureInfo.InvariantCulture)
            );

            /* Invalid phone */
            validationService.Setup(v => v.ValidatePhone(FPhone)).Throws(new InvalidMobileNumberException());
            /* Valid phone */
            validationService.Setup(v => v.ValidatePhone(Phone));
            validationService.Setup(v => v.ValidatePhone(PhoneDuplicate));

            /* Invalid otp */
            validationService.Setup(v => v.ValidateOtp(Phone, FOtp, Email)).Throws(new InvalidMobileOtpException());
            validationService.Setup(v => v.ValidateOtp(PhoneDuplicate, FOtp, Email)).Throws(new InvalidMobileOtpException());
            validationService.Setup(v => v.ValidateOtp(Mobile, FOtp, Email)).Throws(new InvalidMobileOtpException());

            /* Valid otp */
            validationService.Setup(v => v.ValidateOtp(Phone, Otp, Email));
            validationService.Setup(v => v.ValidateOtp(Mobile, Otp, Email));
            validationService.Setup(v => v.ValidateOtp(PhoneDuplicate, Otp, Email));

            /* Invalid account number */
            validationService.Setup(v => v.ValidateAccountNumber(FAccountNumber))
                .Throws(new BankAccountValidationException("Account number"));
            /* Valid account number */
            validationService.Setup(v => v.ValidateAccountNumber(AccountNumber));

            /* Invalid bsb */
            validationService.Setup(v => v.ValidateBsb(FBsb)).Throws(new BankAccountValidationException("Bsb"));
            /* Valid bsb */
            validationService.Setup(v => v.ValidateBsb(Bsb));

            /* Invalid account name */
            validationService.Setup(v => v.ValidateAccountName(FAccountName))
                .Throws(new BankAccountValidationException("Account name"));
            /* Valid account name */
            validationService.Setup(v => v.ValidateAccountName(AccountName));

            /* Invalid post code */
            validationService.Setup(v => v.ValidatePostCode(FPostCode)).Throws(new InvalidPostCodeException());
            /* Valid post code*/
            validationService.Setup(v => v.ValidatePostCode(PostCode));
            
            /* Invalid new password */
            validationService.Setup(v => v.ValidatePassword(FNewPassword)).Throws(new InvalidPasswordException("Your password"));
            /* Valid password*/
            validationService.Setup(v => v.ValidatePassword(NewPassword));

            var fMemberIdStr = "";
            var fHashed = "";
            /* Invalid email verification code */
            validationService.Setup(v => v.ValidateEmailVerificationCode(FEmailVerificationCode, out fMemberIdStr, out fHashed))
                .Throws(new InvalidEmailVerificationCodeException());

            var memberIdStr = MemberId.ToString();
            var otherHashedEmail = "other dummy hashed email";

            /* HashedMail input not equal HashedMail store*/
            validationService.Setup(v =>
                    v.ValidateEmailVerificationCode(OtherFEmailVerificationCode, out memberIdStr, out otherHashedEmail))
                .Throws(new InvalidEmailVerificationCodeException());
            
            /* Valid email verification code */
            var hashedMailStr = HashedEmail;
            validationService.Setup(v =>
                v.ValidateEmailVerificationCode(EmailVerificationCode, out memberIdStr, out hashedMailStr));

            /* Invalid Mobile Link Code */
            validationService.Setup(v =>
                    v.ValidateCheckMobileLinkCode(BadCode))
                .Throws(new BadRequestException("Bad parameters"));

            /* Valid Mobile Link code */
            validationService.Setup(v =>
                v.ValidateCheckMobileLinkCode(Code));

            /*Invalid first name*/
            validationService.Setup(v =>
                v.ValidateName(null, It.IsAny<string>())).Throws(new InvalidNameException("First name"));
            
            /*Invalid last name*/
            validationService.Setup(v =>
                v.ValidateName("abc", null)).Throws(new InvalidNameException("Last name"));
            
            /*Valid name*/
            validationService.Setup(v => v.ValidateName("abc", "xyz"));

            /*Invalid feedback*/
            validationService.Setup(v => v.ValidateFeedback(null)).Throws(new InvalidFeedbackException());
            /*Feedback*/
            validationService.Setup(v => v.ValidateFeedback("abc"));

            /*Invalid app version*/
            validationService.Setup(v => v.ValidateAppVersion(null)).Throws(new InvalidAppVersionException());
            /*App version*/
            validationService.Setup(v => v.ValidateAppVersion("abc"));
            
            /*Invalid device model*/
            validationService.Setup(v => v.ValidateDeviceModel(null)).Throws(new InvalidDeviceModelException());
            /*Device model*/
            validationService.Setup(v => v.ValidateDeviceModel("abc"));
            
            /*Invalid operating system*/
            validationService.Setup(v => v.ValidateOperatingSystem(null)).Throws(new InvalidOperationException());
            /*Operating system*/
            validationService.Setup(v => v.ValidateOperatingSystem("abc"));
            
            /*Invalid operating system*/
            validationService.Setup(v => v.ValidateBuildNumber(null)).Throws(new InvalidBuildNumberException());
            /*Operating system*/
            validationService.Setup(v => v.ValidateBuildNumber("abc"));

            return validationService;
        }

        private static Mock<IMobileOptService> InitMobileOtpServiceMock()
        {
            var mobileOtpServiceMock = new Mock<IMobileOptService>();
            mobileOtpServiceMock.Setup(setup => setup.VerifyMobileOtp(Mobile, FOtp, Email)).Returns(true);

            return mobileOtpServiceMock;
        }

        private static Mock<IMemberBalanceService> InitMemberBalanceServiceMock()
        {
            var memberBalanceService = new Mock<IMemberBalanceService>();

            return memberBalanceService;
        }

        private static Mock<IEmailService> InitEmailServiceMock()
        {
            var emailService = new Mock<IEmailService>();
            return emailService;
        }
        private static Mock<IFeatureToggleService> InitFeatureServiceEnabledMock()
        {
            var featureToggleServiceMock = new Mock<IFeatureToggleService>();
            featureToggleServiceMock.Setup(p => p.IsEnable(It.IsAny<string>())).Returns(true);
            return featureToggleServiceMock;
        }
        private static Mock<IFeatureToggleService> InitFeatureServiceDisabledMock()
        {
            var featureToggleServiceMock = new Mock<IFeatureToggleService>();
            featureToggleServiceMock.Setup(p => p.IsEnable(It.IsAny<string>())).Returns(false);
            return featureToggleServiceMock;
        }

        private static Mock<ITimeService> InitTimeServiceMock()
        {
            var timeService = new Mock<ITimeService>();
            timeService.Setup(t => t.GetCurrentTimestamp()).Returns(CurrentTimeStamp);
            
            return timeService;
        }

        private static Mock<IAwsService> InitAwsServiceMock()
        {
            var awsService = new Mock<IAwsService>();
            return awsService;
        }

        private static Mock<IDatabase> InitRedisDbMock()
        {
            return new Mock<IDatabase>();
        }

        private static Mock<ILeanplumService> InitLeanplumServiceMock()
        {
            return new Mock<ILeanplumService>();
        }

        private static Mock<IFeatureToggleService> InitFeatureToggleServiceMock()
        {
            return new Mock<IFeatureToggleService>();
        }
    }
}