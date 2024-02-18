using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model.Enum;
using SettingsAPI.Service;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class TestMemberBankAccountService
    {
        /* Common init data */
        private const string SaltKey = "saltKey";
        private const string EncryptWithSaltValue = "encrypted salt";
        private const int MemberId = 10000;
        private const int MemberId2 = 11000;

        /* Valid data (true) */

        private const string AccountNumber = "123456789987654321";
        private const string Bsb = "54321";
        private const string AccountName = "dummy";
        private const string MobileOtp = "123456";
        private const string Mobile = "+64 111111111";

        /* Invalid data (false) */

        private const int FMemberId = 10001;
        private const int FMemberId2 = 10002;
        private const string FAccountNumber = "";
        private const string FBsb = "12345";
        private const string FAccountName = "";
        private const string FMobileOtp = "654321";
        private const string Email = "abctest@cashrewards.com";

        [Fact]
        public async Task TestSaveBankAccount()
        {
            var shopGoContex = InitShopGoContext();
            var bankService = InitBankAccountService(shopGoContex);
        
            //Invalid account name
            await Assert.ThrowsAsync<BankAccountValidationException>(() => bankService.SaveBankAccount(
                It.IsAny<int>(), FAccountName, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        
            //Invalid account number
            await Assert.ThrowsAsync<BankAccountValidationException>(() => bankService.SaveBankAccount(
                It.IsAny<int>(), AccountName, It.IsAny<string>(), FAccountNumber, It.IsAny<string>()));
        
            //Invalid bsb
            await Assert.ThrowsAsync<BankAccountValidationException>(() => bankService.SaveBankAccount(
                It.IsAny<int>(), AccountName, FBsb, AccountNumber, It.IsAny<string>()));
        
            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => bankService.SaveBankAccount(
                FMemberId2, AccountName, Bsb, AccountNumber, It.IsAny<string>()));
            
            //Invalid mobile otp
            await Assert.ThrowsAsync<InvalidMobileOtpException>(() => bankService.SaveBankAccount(
                MemberId, AccountName, Bsb, AccountNumber, FMobileOtp));
            
            //Duplicate account
            await Assert.ThrowsAsync<DuplicateBankAccountException>(() => bankService.SaveBankAccount(
                FMemberId, AccountName, Bsb, AccountNumber, MobileOtp));
        
            //Valid
            await bankService.SaveBankAccount(MemberId, AccountName, Bsb, AccountNumber, MobileOtp);
            shopGoContex.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Exactly(2));
        }

        [Fact]
        public async Task TestGetBankAccount()
        {
            var shopGoContex = InitShopGoContext();
            var bankService = InitBankAccountService(shopGoContex);

            //Member bank account not found
            await Assert.ThrowsAsync<MemberBankAccountNotFoundException>(() => bankService.GetBankAccountMasked(FMemberId));

            //Valid
            try
            {
                await bankService.GetBankAccount(MemberId);
            }
            catch (MemberBankAccountNotFoundException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public async Task TestDisconnectBankAccount()
        {
            var shopGoContex = InitShopGoContext();
            var bankService = InitBankAccountService(shopGoContex);
            
            //Invalid
            await Assert.ThrowsAsync<MemberBankAccountNotFoundException>(() => bankService.GetBankAccount(FMemberId));

            //Valid
            await bankService.DisconnectBankAccount(MemberId);
            shopGoContex.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Exactly(2));
        }

        private static MemberBankAccountService InitBankAccountService(IMock<ShopGoContext> shopGoContextMock)
        {
            var validationService = InitValidationService();

            var encryptionServiceMock = InitEncryptionServiceMock();

            var service = new MemberBankAccountService(Mock.Of<IOptions<Settings>>(), shopGoContextMock.Object, encryptionServiceMock.Object,
                validationService.Object);

            return service;
        }

        private static Mock<ShopGoContext> InitShopGoContext()
        {
            var memberBankAccounts = new List<MemberBankAccount>
            {
                new MemberBankAccount
                {
                    AccountNumber = AccountNumber,
                    Bsb = Bsb,
                    BankAccountId = 11111,
                    MemberId = MemberId,
                    AccountName = AccountName,
                    Status = StatusType.Active.GetHashCode()
                },
                new MemberBankAccount
                {
                    AccountNumber = AccountNumber,
                    Bsb = Bsb,
                    BankAccountId = 11111,
                    MemberId = MemberId2,
                    AccountName = AccountName,
                    Status = StatusType.Active.GetHashCode()
                }
            };

            var members = new List<Member>
            {
                new Member
                {
                    MemberId = MemberId,
                    Email = Email,
                    Mobile = Mobile
                },
                new Member
                {
                    MemberId = MemberId2
                },
                new Member
                {
                    MemberId = FMemberId
                }
            };


            var optionsBuilder = new DbContextOptionsBuilder<ShopGoContext>();
            var contextMock = new Mock<ShopGoContext>(optionsBuilder.Options);
            contextMock.Setup(p => p.MemberBankAccount).ReturnsDbSet(memberBankAccounts);
            contextMock.Setup(p => p.Member).ReturnsDbSet(members);

            return contextMock;
        }

        private static Mock<IValidationService> InitValidationService()
        {
            var validationService = new Mock<IValidationService>();

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

            /*Invalid mobile otp*/
            validationService.Setup(v => v.ValidateOtp(Mobile, FMobileOtp, Email)).Throws(new InvalidMobileOtpException());

            return validationService;
        }

        private static Mock<IEncryptionService> InitEncryptionServiceMock()
        {
            var encryptionServiceMock = new Mock<IEncryptionService>();

            encryptionServiceMock.Setup(e => e.EncryptWithSalt(Bsb + "." + AccountName, SaltKey))
                .Returns(EncryptWithSaltValue);

            return encryptionServiceMock;
        }
    }
}