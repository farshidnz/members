using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model.Dto;
using SettingsAPI.Service;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class TestMemberRedeemService
    {
        /* Common init data */
        private const int MemberId = 10000;
        private const int MemberIdCR = 20000;
        private const int MemberIdANZ = 20001;
        private const int OtherMemberId1 = 10002;
        private const int OtherMemberId2 = 10003;
        private const int OtherMemberId3 = 10004;
        private const int OtherMemberId4 = 10005;
        private const int PaymentStatusId = 100;

        private const int ClientIdCR = 1000000;
        private const int ClientIdANZ = 1000034;

        /* Valid data (true) */
        private const string PaymentMethod = "PayPal";
        private const decimal Amount = 15;
        private const decimal Balance = 16;
        private const string AccountNumber = "123456789987654321";
        private const decimal MinRedemptionAmount = (decimal) 10.01;
        private const decimal MaxRedemptionAmount = 5000;
        private const string RefreshToken = "dummy";
        private const string Phone = "+64 111111111";
        private const string MobileOtp = "123456";


        /* Invalid data (false) */
        private const string FPaymentMethod = "FBank";
        private const int FMemberId = 10001;
        private const string FMobileOtp = "654321";
        private const string Email = "abctest@cashrewards.com";

        private Mock<IMemberService> memberServiceMock;
        private Mock<IMemberBalanceService> memberBalanceServiceMock;
        private Mock<IValidationService> validationServiceMock;
        private Mock<ITransactionService> transactionServiceMock;

        [Fact]
        public async Task TestWithDraw()
        {
            var shopContext = InitShopGoContext();
            var memberRedeemService = InitMemberRedeemService(shopContext);

            //Invalid payment method
            await Assert.ThrowsAsync<InvalidPaymentMethodException>(() => memberRedeemService.Withdraw(
                It.IsAny<int>(), It.IsAny<decimal>(), FPaymentMethod, It.IsAny<string>()));

            //Member not found
            await Assert.ThrowsAsync<MemberNotFoundException>(() => memberRedeemService.Withdraw(
                FMemberId, It.IsAny<decimal>(), PaymentMethod, It.IsAny<string>()));

            //Member no available balance cause is validated = false
            await Assert.ThrowsAsync<MemberNoAvailableBalanceException>(() => memberRedeemService.Withdraw(
                OtherMemberId1, It.IsAny<decimal>(), PaymentMethod, It.IsAny<string>()));
            
            //Member no available balance cause  member balance not found
            await Assert.ThrowsAsync<MemberNoAvailableBalanceException>(() => memberRedeemService.Withdraw(
                OtherMemberId2, It.IsAny<decimal>(), PaymentMethod, It.IsAny<string>()));
            
            //Member no available balance cause  AvailableBalance ==null
            await Assert.ThrowsAsync<MemberNoAvailableBalanceException>(() => memberRedeemService.Withdraw(
                OtherMemberId1, It.IsAny<decimal>(), PaymentMethod, It.IsAny<string>()));
            
            //Invalid mobile otp
            await Assert.ThrowsAsync<InvalidMobileOtpException>(() => memberRedeemService.Withdraw(
                MemberId, It.IsAny<decimal>(), PaymentMethod, FMobileOtp));
            
            //Invalid amount
            //Case amount < 0
            await Assert.ThrowsAsync<InvalidAmountException>(() => memberRedeemService.Withdraw(
                MemberId, -1, PaymentMethod, MobileOtp));
            
            //Case amount < Convert.ToDecimal(_options.Value.MinRedemptionAmount)
            await Assert.ThrowsAsync<InvalidAmountException>(() => memberRedeemService.Withdraw(
                MemberId, 9, PaymentMethod, MobileOtp));
            
            //Case Math.Round(balance, 2) < amount
            await Assert.ThrowsAsync<InvalidAmountException>(() => memberRedeemService.Withdraw(
                MemberId, 17, PaymentMethod, MobileOtp));
            
            //Case amount >= Convert.ToInt32(_options.Value.MaxRedemptionAmount)
            await Assert.ThrowsAsync<InvalidAmountException>(() => memberRedeemService.Withdraw(
                MemberId, 5001, PaymentMethod, MobileOtp));
            
            //MemberNotRedeemException
            await Assert.ThrowsAsync<MemberNotRedeemException>(() => memberRedeemService.Withdraw(
                OtherMemberId3, Amount, PaymentMethod, MobileOtp));
            
            //Validate paypal (case payment method is paypal)
            await Assert.ThrowsAsync<PaypalAccountHasNotBeenVerifiedException>(() => memberRedeemService.Withdraw(
                OtherMemberId4, Amount, PaymentMethod, MobileOtp));
            
            //Valid
            await memberRedeemService.Withdraw(MemberId, Amount, PaymentMethod, MobileOtp);
            shopContext.Verify(c => c.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        private Mock<MemberRedeemService> InitMemberRedeemServiceMock(IMock<ShopGoContext> shopGoContextMock, Func<Mock<IMemberBalanceService>> memberBalanceServiceFunc = null, Func<Mock<ITransactionService>> transactionServiceFunc = null, Func<int?, Mock<IMemberService>> memberServiceFunc = null, int? personId = 5555)
        {
            var memberPaypalAccountServiceMock = InitMemberPaypalServiceMock();
            var emailServiceMock = InitEmailServiceMock();
            var memberBankAccountServiceMock = InitMemberBankAccountServiceMock();
            
            this.memberServiceMock = memberServiceFunc?.Invoke(personId) ?? InitMemberServiceMock(personId);
            this.memberBalanceServiceMock = memberBalanceServiceFunc?.Invoke() ?? InitMemberBalanceServiceMock();
            this.validationServiceMock = InitValidationServiceMock();
            this.transactionServiceMock = transactionServiceFunc?.Invoke() ?? InitTransactionServiceMock();

            return new Mock<MemberRedeemService>(shopGoContextMock.Object, memberPaypalAccountServiceMock.Object,
                    this.memberBalanceServiceMock.Object, this.transactionServiceMock.Object,
                    this.validationServiceMock.Object, emailServiceMock.Object,
                    memberBankAccountServiceMock.Object, this.memberServiceMock.Object);
        }

        private MemberRedeemService InitMemberRedeemService(IMock<ShopGoContext> shopGoContextMock, Func<Mock<IMemberBalanceService>> memberBalanceServiceFunc = null, Func<Mock<ITransactionService>> transactionServiceFunc = null, Func<int?, Mock<IMemberService>> memberServiceFunc = null)
        {
            return InitMemberRedeemServiceMock(shopGoContextMock, memberBalanceServiceFunc, transactionServiceFunc, memberServiceFunc).Object;
        }

        private static Mock<ShopGoContext> InitShopGoContext()
        {
            // Member paypal accounts
            var memberPaypalAccounts = new List<MemberPaypalAccount>
            {
                new MemberPaypalAccount
                {
                    MemberId = MemberId
                }
            };

            // Members
            var members = new List<Member>
            {
                new Member
                {
                    MemberId = MemberId,
                    IsAvailable = true,
                    IsValidated = true,
                    Email = Email,
                    Mobile = Phone
                },
                new Member
                {
                    MemberId = OtherMemberId1,
                    IsAvailable = false,
                    IsValidated = false
                },
                new Member
                {
                    MemberId = OtherMemberId2,
                    IsAvailable = false,
                    IsValidated = false
                },
                new Member
                {
                    MemberId = OtherMemberId3,
                    IsAvailable = true,
                    IsValidated = true
                },
                new Member
                {
                    MemberId = OtherMemberId4,
                    IsAvailable = true,
                    IsValidated = true
                },
                new Member
                {
                    MemberId = MemberIdCR,
                    IsAvailable = true,
                    IsValidated = true
                }
            };
            // Member balances
            var memberBalances = new List<MemberBalanceView>
            {
                new MemberBalanceView
                {
                    MemberID = MemberId,
                    AvailableBalance = Balance
                },
                new MemberBalanceView
                {
                    MemberID = OtherMemberId1,
                    AvailableBalance = null
                }
            };

            //Member redeem
            var memberRedeems = new List<MemberRedeem>
            {
                new MemberRedeem
                {
                    MemberId = MemberId,
                    PaymentMethodId = PaymentStatusId
                }
            };

            var memberBankAccounts = new List<MemberBankAccount>
            {
                new MemberBankAccount
                {
                    MemberId = MemberId,
                    AccountNumber = AccountNumber
                }
            };

            var optionsBuilder = new DbContextOptionsBuilder<ShopGoContext>();

            var contextMock = new Mock<ShopGoContext>(optionsBuilder.Options);
            contextMock.Setup(c => c.MemberPaypalAccount).ReturnsDbSet(memberPaypalAccounts);
            contextMock.Setup(c => c.Member).ReturnsDbSet(members);
            contextMock.Setup(c => c.MemberBalanceView).ReturnsDbSet(memberBalances);
            contextMock.Setup(c => c.MemberRedeem).ReturnsDbSet(memberRedeems);
            contextMock.Setup(c => c.MemberBankAccount).ReturnsDbSet(memberBankAccounts);

            return contextMock;
        }

        private static Mock<IOptions<Settings>> InitOptionsMock()
        {
            var optionsMock = new Mock<IOptions<Settings>>();
            var settings = new Settings
            {
                PaypalTokenService = "https://api.paypal.com/v1/identity/openidconnect/tokenservice",
                MinRedemptionAmount = MinRedemptionAmount,
                MaxRedemptionAmount = MaxRedemptionAmount
            };
            optionsMock.Setup(o => o.Value).Returns(settings);

            return optionsMock;
        }

        private static Mock<IMemberPaypalAccountService> InitMemberPaypalServiceMock()
        {
            var mock = new Mock<IMemberPaypalAccountService>();
            var memberPayPanAccount = new MemberPaypalAccount
            {
                MemberId = MemberId,
                VerifiedAccount = true
            };

            var otherMemberPayPanAccount1 = new MemberPaypalAccount
            {
                MemberId = OtherMemberId3,
                VerifiedAccount = false,
                RefreshToken = null
            };

            var otherMemberPayPanAccount2 = new MemberPaypalAccount
            {
                MemberId = OtherMemberId4,
                VerifiedAccount = false,
                RefreshToken = RefreshToken
            };

            mock.Setup(m => m.GetActiveMemberPaypalAccount(MemberId))
                .Returns(Task.FromResult<MemberPaypalAccount>(memberPayPanAccount));

            mock.Setup(m => m.GetActiveMemberPaypalAccount(OtherMemberId3))
                .Returns(Task.FromResult<MemberPaypalAccount>(otherMemberPayPanAccount1));

            mock.Setup(m => m.GetActiveMemberPaypalAccount(OtherMemberId4))
                .Returns(Task.FromResult<MemberPaypalAccount>(otherMemberPayPanAccount2));

            mock.Setup(m => m.GetActiveMemberPaypalAccount(MemberIdCR))
                .Returns(Task.FromResult<MemberPaypalAccount>(memberPayPanAccount));
            
            return mock;
        }

        private static Mock<IMemberBalanceService> InitMemberBalanceServiceMock()
        {
            var mock = new Mock<IMemberBalanceService>();

            var memberBalance = new MemberBalanceView
            {
                MemberID = MemberId,
                AvailableBalance = Balance
            };
            var memberBalanceOther = new MemberBalanceView
            {
                MemberID = OtherMemberId3,
                AvailableBalance = Balance
            };
            var memberBalanceOther2 = new MemberBalanceView
            {
                MemberID = OtherMemberId4,
                AvailableBalance = Balance
            };

            // this weirdness is required because the return expects a IList... cannot cast List<T> -> IList<T> directly... 
            IList<MemberBalanceView> theMemberBalanceList = new List<MemberBalanceView>() { memberBalance };
            IList<MemberBalanceView> theMemberBalanceOtherList = new List<MemberBalanceView>() { memberBalanceOther };
            IList<MemberBalanceView> theMemberBalanceOther2List = new List<MemberBalanceView>() { memberBalanceOther2 };

            mock.Setup(m => m.GetBalanceViews(new int[] { MemberId }, It.IsAny<bool>())).Returns(Task.FromResult(theMemberBalanceList));
            mock.Setup(m => m.GetBalanceViews(new int[] { OtherMemberId3 }, It.IsAny<bool>())).Returns(Task.FromResult(theMemberBalanceOtherList));
            mock.Setup(m => m.GetBalanceViews(new int[] { OtherMemberId4 }, It.IsAny<bool>())).Returns(Task.FromResult(theMemberBalanceOther2List));

            return mock;
        }

        private static Mock<ITransactionService> InitTransactionServiceMock()
        {
            var mock = new Mock<ITransactionService>();
            mock.Setup(m => m.HasApprovedPurchases(MemberId)).Returns(Task.FromResult(true));
            mock.Setup(m => m.HasApprovedPurchases(OtherMemberId3)).Returns(Task.FromResult(false));
            mock.Setup(m => m.HasApprovedPurchases(OtherMemberId4)).Returns(Task.FromResult(true));

            mock.Setup(m => m.HasApprovedPurchases(MemberIdCR)).Returns(Task.FromResult(true));
            mock.Setup(m => m.HasApprovedPurchases(MemberIdANZ)).Returns(Task.FromResult(true));

            return mock;
        }

        private static Mock<IValidationService> InitValidationServiceMock()
        {
            var mock = new Mock<IValidationService>();

            //Invalid payment method 
            mock.Setup(m => m.ValidatePaymentMethod(FPaymentMethod)).Throws<InvalidPaymentMethodException>();

            //Valid payment method
            mock.Setup(m => m.ValidatePaymentMethod(PaymentMethod));

            //Invalid amount
            mock.Setup(m => m.ValidateAmount(-1, It.IsAny<decimal>())).Throws(new InvalidAmountException(
                string.Format(AppMessage.FieldInvalid, "Amount")));

            mock.Setup(m => m.ValidateAmount(9, It.IsAny<decimal>())).Throws(new InvalidAmountException(
                string.Format(AppMessage.FieldInvalid, "Amount")));

            mock.Setup(m => m.ValidateAmount(17, Balance)).Throws(new InvalidAmountException(
                AppMessage.AmountGreaterThanAvailableRewards));

            mock.Setup(m => m.ValidateAmount(5001, Balance)).Throws(new InvalidAmountException(
                AppMessage.AmountGreaterThanMaximumLimit));

            //Valid amount
            mock.Setup(m => m.ValidateAmount(Amount, Balance));
            
            //Invalid mobile otp
            mock.Setup(m => m.ValidateOtp(Phone, FMobileOtp, Email)).Throws(new InvalidMobileOtpException());

            //Valid mobile otp
            mock.Setup(m => m.ValidateOtp(Phone, MobileOtp, Email));

            return mock;
        }

        private static Mock<IEmailService> InitEmailServiceMock()
        {
            var mock = new Mock<IEmailService>();
            return mock;
        }

        private static Mock<IMemberBankAccountService> InitMemberBankAccountServiceMock()
        {
            var mock = new Mock<IMemberBankAccountService>();

            var memberBankAccount = new MemberBankAccountInfo
            {
                AccountNumber = AccountNumber
            };
            mock.Setup(m => m.GetBankAccountMasked(MemberId)).Returns(Task.FromResult(memberBankAccount));
            return mock;
        }

        private static MembershipDetail GetMembershipDetailForMemberId(int memberId, int? personId)
        {
            return new MembershipDetail()
            {
                Items = new List<MemberShipItem>()
                {
                    new MemberShipItem()
                    {
                        PersonId = personId,
                        MemberId = memberId,
                        ClientId = ClientIdCR
                    }
                }
            };
        }

        private static void SetupMembershipInfo(Mock<IMemberService> mock, int memberId, int? personId)
        {
            mock.Setup(m => m.GetMembershipInfo(memberId)).Returns(Task.FromResult(GetMembershipDetailForMemberId(memberId, personId)));
        }

        private static Mock<IMemberService> InitMemberServiceMock(int? personId)
        {
            var mock = new Mock<IMemberService>();

            var theMemberShipDetails = new MembershipDetail()
            {
                Items = new List<MemberShipItem>()
                {
                    new MemberShipItem()
                    {
                        PersonId = personId,
                        MemberId = MemberIdCR,
                        ClientId = ClientIdCR
                    },
                    new MemberShipItem()
                    {
                        PersonId = personId,
                        MemberId = MemberIdANZ,
                        ClientId = ClientIdANZ
                    }
                }
            };
            mock.Setup(m => m.GetMembershipInfo(MemberIdCR)).Returns(Task.FromResult(theMemberShipDetails));

            SetupMembershipInfo(mock, MemberId, personId);
            SetupMembershipInfo(mock, OtherMemberId1, personId);
            SetupMembershipInfo(mock, OtherMemberId2, personId);
            SetupMembershipInfo(mock, OtherMemberId3, personId);
            SetupMembershipInfo(mock, OtherMemberId4, personId);

            return mock;
        }

        private Mock<IMemberBalanceService> InitMemberBalanceServiceMockMultiClients(params decimal?[] balance)
        {
            var mock = new Mock<IMemberBalanceService>();

            IList<MemberBalanceView> theBalanceViews = new List<MemberBalanceView>();
            for (var i = 0; i < balance.Length; i++)
            {
                int memberId = -1;
                switch (i)
                {
                    case 0:
                        memberId = MemberIdCR; break;
                    case 1:
                        memberId = MemberIdANZ; break;
                    default:
                        memberId = MemberIdANZ + i - 1; break;
                }

                var memberBalance = new MemberBalanceView()
                {
                    MemberID = memberId,
                    AvailableBalance = balance[i]
                };

                theBalanceViews.Add(memberBalance);
            }

            mock.Setup(m => m.GetBalanceViews(It.IsAny<int[]>(), It.IsAny<bool>())).Returns(Task.FromResult(theBalanceViews));

            return mock;
        }

        private static Mock<IMemberBalanceService> InitMemberBalanceServiceMockClient(decimal? crBalance = 50)
        {
            var mock = new Mock<IMemberBalanceService>();

            var memberBalance = new MemberBalanceView()
            {
                MemberID = MemberIdCR,
                AvailableBalance = crBalance
            };

            IList<MemberBalanceView> theBalanceViews = new List<MemberBalanceView>() { memberBalance };
            mock.Setup(m => m.GetBalanceViews(It.IsAny<int[]>(), It.IsAny<bool>())).Returns(Task.FromResult(theBalanceViews));

            return mock;
        }

        [Fact]
        public async Task WhenICallWithdraw_ThenIShouldCallMemberServicesGetMemberShipPassingInTheCorrectInformation()
        {
            var shopContext = InitShopGoContext();
            var memberRedeemService = InitMemberRedeemService(shopContext);

            await memberRedeemService.Withdraw(MemberId, 11m, PaymentMethod, "123546");

            this.memberServiceMock.Verify(m => m.GetMembershipInfo(MemberId), Times.Once);
        }

        [Fact]
        public async Task WhenICallWithdraw_ThenIShouldGetBalanceInformationForAllMemberShips()
        {
            var shopContext = InitShopGoContext();
            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(50, 50));

            await memberRedeemService.Withdraw(MemberIdCR, 10m, PaymentMethod, "123456");

            this.memberBalanceServiceMock.Verify(m => m.GetBalanceViews(new int[] { MemberIdCR, MemberIdANZ }, It.IsAny<bool>()), Times.Once);
        }

        [Theory]
        [InlineData(50, 60, 110)]
        [InlineData(50, 30, 80)]
        [InlineData(30, 50, 80)]
        [InlineData(5.01, 5, 10.01)]
        public async Task WhenICallWithdrawAndIHaveBalancesAcrossMemberships_ThenIShouldCallValidateAmountWithTheAppropriateInformation(decimal crBalance, decimal anzBalance, decimal totalBalance)
        {
            var shopContext = InitShopGoContext();
            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(crBalance, anzBalance));

            await memberRedeemService.Withdraw(MemberIdCR, 11m, PaymentMethod, "123456");

            this.validationServiceMock.Verify(m => m.ValidateAmount(It.IsAny<decimal>(), totalBalance), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenICallWithdraw_IsPartialShouldBeSetAccordingIfThereAreBalancesWithdrawnFromMultipleAccountsOrNot(bool multipleAccounts)
        {
            var shopContext = InitShopGoContext();

            var memberRedeemCalls = new List<MemberRedeem>();
            shopContext.Setup(c => c.MemberRedeem.AddAsync(It.IsAny<MemberRedeem>(), It.IsAny<CancellationToken>())).Callback<MemberRedeem, CancellationToken>((r, c) => memberRedeemCalls.Add(r));

            MemberRedeemService memberRedeemService = null;

            if (multipleAccounts)
            {
                memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(50, 50));
            }
            else
            {
                memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockClient());
            }

            await memberRedeemService.Withdraw(MemberIdCR, 100, PaymentMethod, "123456");

            if (multipleAccounts)
            {
                foreach (var currentRedeemCall in memberRedeemCalls)
                {
                    currentRedeemCall.IsPartial.Should().BeTrue();
                }
            }
            else
            {
                memberRedeemCalls[0].IsPartial.Should().BeFalse();
            }
        }

        [Theory]
        [InlineData(80, 20, 100, 80, 20)]
        [InlineData(80, 40, 100, 80, 20)]
        [InlineData(0, 140, 100, 0, 100)]
        [InlineData(140, 0, 100, 100, 0)]
        [InlineData(5.01, 5, 10.01, 5.01, 5)]
        [InlineData(100, 50, 20, 20, 0)]
        [InlineData(140.1264, 0, 140.13, 140.12, 0)]
        [InlineData(140.1222, 0, 140.12, 140.12, 0)]
        public async Task GivenICallWithdrawAndIHaveBalancesAcrossCRAndANZMemberships_ThenIShouldDrawDownTheCRBalanceFirst_ThenDrawDownTheANZBalance(decimal crBalance, decimal anzBalance, decimal withdrawAmount, decimal crAmount, decimal anzAmount)
        {
            var crZero = crAmount == 0;
            var anzZero = anzAmount == 0;

            var shopContext = InitShopGoContext();

            var memberRedeemCalls = new List<MemberRedeem>();
            shopContext.Setup(c => c.MemberRedeem.AddAsync(It.IsAny<MemberRedeem>(), It.IsAny<CancellationToken>())).Callback<MemberRedeem, CancellationToken>((r, c) => memberRedeemCalls.Add(r));

            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(crBalance, anzBalance));

            await memberRedeemService.Withdraw(MemberIdCR, withdrawAmount, PaymentMethod, "123456");

            var onlyOne = crZero || anzZero;

            memberRedeemCalls.Count.Should().Be(onlyOne ? 1 : 2);

            if (!crZero)
            {
                var crMemberRedeem = memberRedeemCalls.Where(m => m.MemberId == MemberIdCR).FirstOrDefault();
                crMemberRedeem.AmountRequested.Should().Be(crAmount);
            }

            if (!anzZero)
            {
                var anzMemberRedeem = memberRedeemCalls.Where(m => m.MemberId == MemberIdANZ).FirstOrDefault();
                anzMemberRedeem.AmountRequested.Should().Be(anzAmount);
            }

            memberRedeemCalls.First().IsPartial.Should().Be(!onlyOne);
        }

        [Theory]
        [InlineData(20, 30, 40, 60, 20, 30, 10)]
        [InlineData(100, 100, 100, 250, 100, 100, 50)]
        [InlineData(20, 30, 40, 40, 20, 20, 0)]
        [InlineData(50, 10, 20, 55, 50, 5, 0)]
        [InlineData(100, 50, 50, 20, 20, 0, 0)]
        [InlineData(50, 0, 20, 60, 50, 0, 10)]
        [InlineData(0, 10, 20, 15, 0, 10, 5)]
        [InlineData(50, 20, 0, 55, 50, 5, 0)]
        public async Task GivenICallWithdrawAndIHaveBalancesAcrossMultipleMemberships_ThenIShouldDrawDownTheCRBalanceFirst_ThenDrawDownTheOtherBalances(decimal balance1, decimal balance2, decimal balance3, decimal withdrawAmount, decimal amount1, decimal amount2, decimal amount3)
        {
            var shopContext = InitShopGoContext();

            var memberRedeemCalls = new List<MemberRedeem>();
            shopContext.Setup(c => c.MemberRedeem.AddAsync(It.IsAny<MemberRedeem>(), It.IsAny<CancellationToken>())).Callback<MemberRedeem, CancellationToken>((r, c) => memberRedeemCalls.Add(r));

            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(balance1, balance2, balance3));

            await memberRedeemService.Withdraw(MemberIdCR, withdrawAmount, PaymentMethod, "123456");

            var amountRequestedChecks = new Dictionary<int, decimal>();
            var theIndex = 0;
            if (amount1 != 0)
            {
                amountRequestedChecks.Add(theIndex++, amount1);
            }
            if (amount2 != 0)
            {
                amountRequestedChecks.Add(theIndex++, amount2);
            }
            if (amount3 != 0)
            {
                amountRequestedChecks.Add(theIndex++, amount3);
            }

            foreach (var currentRequestedCheck in amountRequestedChecks)
            {
                memberRedeemCalls[currentRequestedCheck.Key].AmountRequested.Should().Be(currentRequestedCheck.Value);
                memberRedeemCalls[currentRequestedCheck.Key].IsPartial.Should().Be(theIndex > 1);
            }
        }

        [Fact]
        public async Task WhenICallWithdraw_TheWithdrawalIdShouldBeTheSameForBothMemberRedeemRecords_AndBeACombinationOfThePersonIdAndDate()
        {
            var thePersonId = 123456;
            var theDate = DateTime.UtcNow;

            var shopContext = InitShopGoContext();

            var memberRedeemCalls = new List<MemberRedeem>();
            shopContext.Setup(c => c.MemberRedeem.AddAsync(It.IsAny<MemberRedeem>(), It.IsAny<CancellationToken>())).Callback<MemberRedeem, CancellationToken>((r, c) => memberRedeemCalls.Add(r));

            var memberRedeemServiceMock = InitMemberRedeemServiceMock(shopContext, () => InitMemberBalanceServiceMockMultiClients(50, 50), personId: thePersonId);
            memberRedeemServiceMock.Setup(m => m.GetDateTime()).Returns(theDate);

            var memberRedeemService = memberRedeemServiceMock.Object;

            await memberRedeemService.Withdraw(MemberIdCR, 100, PaymentMethod, "123456");

            var theWithdrawalId = memberRedeemCalls.FirstOrDefault().WithdrawalId;

            theWithdrawalId.Should().NotBeNullOrEmpty();

            theWithdrawalId.Should().Be($"{thePersonId}-{theDate.ToString("yyyyMMddHHmmss")}");

            foreach (var currentRedeemCall in memberRedeemCalls)
            {
                currentRedeemCall.WithdrawalId.Should().Be(theWithdrawalId);
            }
        }

        [Fact]
        public async Task WhenICallWithdraw_AndThereIsNoPersonIdAvailable_ThenTheWithdrawalIdShouldBeACombinationOfTheMemberIdAndDate()
        {
            var theDate = DateTime.UtcNow;

            var shopContext = InitShopGoContext();

            var memberRedeemCalls = new List<MemberRedeem>();
            shopContext.Setup(c => c.MemberRedeem.AddAsync(It.IsAny<MemberRedeem>(), It.IsAny<CancellationToken>())).Callback<MemberRedeem, CancellationToken>((r, c) => memberRedeemCalls.Add(r));

            var memberRedeemServiceMock = InitMemberRedeemServiceMock(shopContext, () => InitMemberBalanceServiceMockMultiClients(50, 50), personId: null);
            memberRedeemServiceMock.Setup(m => m.GetDateTime()).Returns(theDate);

            var memberRedeemService = memberRedeemServiceMock.Object;

            await memberRedeemService.Withdraw(MemberIdCR, 100, PaymentMethod, "123456");

            var theWithdrawalId = memberRedeemCalls.FirstOrDefault().WithdrawalId;

            theWithdrawalId.Should().NotBeNullOrEmpty();

            theWithdrawalId.Should().Be($"{MemberIdCR}-{theDate.ToString("yyyyMMddHHmmss")}");

            foreach (var currentRedeemCall in memberRedeemCalls)
            {
                currentRedeemCall.WithdrawalId.Should().Be(theWithdrawalId);
            }
        }

        [Fact]
        public void WhenICallWithdraw_AndMembersAvailableBalanceIsNull_ThenMemberNoAvailableBalanceExceptionShouldBeRaised()
        {
            var shopContext = InitShopGoContext();
            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockClient(null));

            memberRedeemService.Invoking(m => m.Withdraw(MemberIdCR, 100, PaymentMethod, "123456")).Should().ThrowAsync<MemberNoAvailableBalanceException>();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void WhenICallWithdraw_AndMultiMembersAvailableBalanceIsNull_ThenMemberNoAvailableBalanceExceptionShouldBeRaised(bool crNull, bool anzNull)
        {
            var shopContext = InitShopGoContext();
            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(crNull ? (decimal?)null : 50, anzNull ? (decimal?)null : 50));

            memberRedeemService.Invoking(m => m.Withdraw(MemberIdCR, 100, PaymentMethod, "123456")).Should().ThrowAsync<MemberNoAvailableBalanceException>();
        }

        [Fact]
        public async Task WhenIOnlyHaveApprovedANZTransactions_ThenIShouldBeAbleToWithdrawFromTheANZAccount()
        {
            var theAmount = 15m;

            var shopContext = InitShopGoContext();

            var memberRedeemCalls = new List<MemberRedeem>();
            shopContext.Setup(c => c.MemberRedeem.AddAsync(It.IsAny<MemberRedeem>(), It.IsAny<CancellationToken>())).Callback<MemberRedeem, CancellationToken>((r, c) => memberRedeemCalls.Add(r));

            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(0, 30), () =>
            {
                var mock = new Mock<ITransactionService>();
                mock.Setup(m => m.HasApprovedPurchases(MemberIdANZ)).Returns(Task.FromResult(true));

                return mock;
            });

            await memberRedeemService.Withdraw(MemberIdCR, theAmount, PaymentMethod, "123456");

            memberRedeemCalls.Count.Should().Be(1);
            memberRedeemCalls[0].MemberId.Should().Be(MemberIdANZ);
            memberRedeemCalls[0].AmountRequested.Should().Be(theAmount);
        }

        [Fact]
        public void WhenIHaveNoApprovedTransactions_ThenIShouldThrowMemberNotRedeemException()
        {
            var shopContext = InitShopGoContext();

            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(0, 30), () => new Mock<ITransactionService>());

            memberRedeemService.Invoking(m => m.Withdraw(MemberIdCR, 20, PaymentMethod, "123456")).Should().ThrowAsync<MemberNotRedeemException>();
        }

        [Theory]
        [InlineData(true, true, true, 1)]
        [InlineData(true, true, false, 1)]
        [InlineData(true, false, true, 1)]
        [InlineData(true, false, false, 1)]
        [InlineData(false, true, true, 2)]
        [InlineData(false, true, false, 2)]
        [InlineData(false, false, true, 3)]
        public async Task WhenICallWithdrawAndIHaveBalancesAcrossMemberships_ThenIShouldOnlyCheckForApprovedPurchasesWhenPreviousMembershipIsFalse(bool crTransactionExists, bool anzTransactionExists, bool otherTransactionExists, int amountChecked)
        {
            var shopContext = InitShopGoContext();
            var memberRedeemService = InitMemberRedeemService(shopContext, () => InitMemberBalanceServiceMockMultiClients(50, 50, 50), () =>
            {
                var mock = new Mock<ITransactionService>();

                if (crTransactionExists)
                    mock.Setup(m => m.HasApprovedPurchases(MemberIdCR)).Returns(Task.FromResult(true));

                if (anzTransactionExists)
                    mock.Setup(m => m.HasApprovedPurchases(MemberIdANZ)).Returns(Task.FromResult(true));

                if (otherTransactionExists)
                    mock.Setup(m => m.HasApprovedPurchases(MemberIdANZ + 1)).Returns(Task.FromResult(true));

                return mock;
            }, (personId) =>
            {
                var mock = new Mock<IMemberService>();

                var theMemberShipDetails = new MembershipDetail()
                {
                    Items = new List<MemberShipItem>()
                    {
                        new MemberShipItem()
                        {
                            PersonId = personId,
                            MemberId = MemberIdCR,
                            ClientId = ClientIdCR
                        },
                        new MemberShipItem()
                        {
                            PersonId = personId,
                            MemberId = MemberIdANZ,
                            ClientId = ClientIdANZ
                        },
                        new MemberShipItem()
                        {
                            PersonId = personId,
                            MemberId = MemberIdANZ + 1,
                            ClientId = ClientIdCR
                        }
                    }
                };
                mock.Setup(m => m.GetMembershipInfo(MemberIdCR)).Returns(Task.FromResult(theMemberShipDetails));

                return mock;
            });

            await memberRedeemService.Withdraw(MemberIdCR, 11m, PaymentMethod, "123456");

            this.transactionServiceMock.Verify(m => m.HasApprovedPurchases(It.IsAny<int>()), Times.Exactly(amountChecked));
        }
    }
}