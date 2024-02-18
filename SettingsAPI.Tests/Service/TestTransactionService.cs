using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using SettingsAPI.Model.Enum;
using SettingsAPI.Service;
using Xunit;
using static SettingsAPI.Common.Constant;

namespace SettingsAPI.Tests.Service
{
    public class TestTransactionService
    {
        /* Common init data */
        private const int MemberId = 10000;
        private const int MemberIdANZ = 10001;
        private const int ApprovedTransactionId = 1111;
        private const int PendingTransactionId = 2222;
        private const int DeclinedTransactionId = 3333;
        private const int OverdueTransactionId = 4444;
        private const int RAFTransactionId = 5555;

        /* Valid data (true) */
        private const int Limit = 10;
        private const int Offset = 0;
        private const string DateFromStr = "2020-11-01";
        private const string DateToStr = "2020-11-10";
        private const string OrderBy = "Date";
        private const string SortDirection = "Asc";

        private const string Email = "abc@gmail.com";
        private const string Gender = "Female";
        private const string DateOfBirth = "1993-05-05";
        private const int PremiumStatus = 1;
        private DateTime PremiumDateJoined = DateTime.Parse("2021/08/12");

        /* Invalid data (false) */

        private const int FLimit = 0;
        private const int FOffset = -1;
        private const string FDateFromStr = "2020-11/01";
        private const string FDateToStr = "2020-11/02";
        private const string FOrderBy = "dummy";
        private const string FSortDirection = "dummy";

        private const string FEmail = "abcgmail.com";
        private const string FGender = "abc";
        private const string FDateOfBirth = "1993-05/05";
        private const int FPremiumStatus = 0;

        [Fact]
        public async Task TestGetTransactions()
        {
            var shopGoContext = InitShopGoContext();
            var transactionService = InitTransactionService(shopGoContext, null);
            /* Invalid parameter */

            //Invalid limit
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactions(
                It.IsAny<int>(), FLimit, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            ));

            //Invalid offset
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactions(
                 It.IsAny<int>(), Limit, FOffset, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            ));

            //Invalid date from
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactions(
                 It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), FDateFromStr,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            ));

            //Invalid date to
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactions(
                 It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                FDateToStr, It.IsAny<string>(), It.IsAny<string>()
            ));


            //Invalid date from and date to (date from > date to)
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactions(
                 It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                "2020-10-31", It.IsAny<string>(), It.IsAny<string>()
            ));

            //Invalid order by
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactions(
                 It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                DateToStr, FOrderBy, It.IsAny<string>()
            ));

            //Invalid sort direction
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactions(
                 It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                DateToStr, OrderBy, FSortDirection
            ));

            //Valid
            try
            {
                var result = await transactionService.GetTransactions(
                     MemberId, Limit, Offset, It.IsAny<string>(), DateFromStr,
                    DateToStr, OrderBy, SortDirection
                );

                var transactions = result.Data;

                var approved = transactions.Where(t => t.TransactionId == ApprovedTransactionId).First();
                var pending = transactions.Where(t => t.TransactionId == PendingTransactionId).First();
                var declined = transactions.Where(t => t.TransactionId == DeclinedTransactionId).First();
                var raf = transactions.Where(t => t.TransactionId == RAFTransactionId).First();
                var overdue = transactions.Where(t => t.TransactionId == OverdueTransactionId).First();

                approved.Status.Should().Be("Approved");
                approved.IsOverdue.Should().BeFalse();
                approved.MaxStatus.Should().Be("Active");
                pending.Status.Should().Be("Pending");
                pending.IsOverdue.Should().BeFalse();
                pending.MaxStatus.Should().Be("NotApplicable");
                declined.Status.Should().Be("Declined");
                declined.IsOverdue.Should().BeFalse();
                raf.Status.Should().Be("Pending");
                raf.IsOverdue.Should().BeFalse();
                overdue.Status.Should().Be("Pending");
                overdue.MaxStatus.Should().Be("NotApplicable");
                overdue.IsOverdue.Should().BeTrue();

            }
            catch (InvalidQueryConditionException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public async Task TestTransactionsForPremiumMembers()
        {
            var shopGoContext = InitShopGoContext();
            var transactionService = InitTransactionService(shopGoContext, () => InitMemberServiceMock(PremiumStatus, PremiumDateJoined));

            var result = await transactionService.GetTransactions(
                 MemberId, Limit, Offset, It.IsAny<string>(), null,
                null, OrderBy, SortDirection
            );

            var transactions = result.Data;

            transactions.Count.Should().Be(5);
        }

        [Fact]
        public async Task TestTransactionsForNonPremiumMembers()
        {
            var shopGoContext = InitShopGoContext();
            var transactionService = InitTransactionService(shopGoContext);

            var result = await transactionService.GetTransactions(
                 MemberId, Limit, Offset, It.IsAny<string>(), DateFromStr,
                DateToStr, OrderBy, SortDirection
            );

            var transactions = result.Data;

            transactions.Count.Should().Be(5);
        }

        [Theory]
        [InlineData(TransactionType.Cashback, CashbackTransactionType.CashbackClaim, 123456, Networks.InStoreNetwork, CashbackFlag.CashbackInStore)]
        [InlineData(TransactionType.Cashback, CashbackTransactionType.CashbackClaim, 123456, 10000, CashbackFlag.CashbackOnline)]
        [InlineData(TransactionType.Cashback, CashbackTransactionType.RafFriend, Constant.CashRewardsReferAMateMerchantId, 10000, CashbackFlag.RafFriend)]
        [InlineData(TransactionType.Cashback, CashbackTransactionType.RafReferrer, Constant.CashRewardsReferAMateMerchantId, 10000, CashbackFlag.RafReferrer)]
        [InlineData(TransactionType.Cashback, CashbackTransactionType.Promo5DollarSignupBonus, Constant.CashRewardsBonusMerchantId, 10000, CashbackFlag.Bonus)]
        [InlineData(TransactionType.Cashback, CashbackTransactionType.None, Constant.CashRewardsPromotionalBonusMerchantId, 10000, CashbackFlag.Bonus)]
        [InlineData(TransactionType.Cashback, CashbackTransactionType.PromoMS, Constant.CashRewardsWelcomeBonusMerchantId, 10000, CashbackFlag.Bonus)]
        [InlineData(TransactionType.Withdrawal, CashbackTransactionType.None, 123456, 10000, CashbackFlag.None)]
        [InlineData(TransactionType.Savings, CashbackTransactionType.None, 123456, 10000, CashbackFlag.None)]
        public async Task TestGetTransactions_ReturnsCorrectCashbackFlag(
            TransactionType transactionType, CashbackTransactionType cashbackTransactionType, 
            int merchantId, int networkId, CashbackFlag expectedCashbackFlag)
        {
            var transactionId = 12345678;
            var transaction = new TransactionView
            {
                TransactionId = transactionId,
                MemberId = MemberId,
                MerchantId = merchantId,
                NetworkId = networkId,
                SaleDate = new DateTime(2020, 11, 02),
                TransactionStatus = "Approved",
                TransactionType = (int)transactionType,
                TransactionTypeId = (int)cashbackTransactionType,
                SaleDateAest = new DateTime(2020, 11, 02)
            };

            var shopGoContext = InitShopGoContext(transaction);
            var transactionService = InitTransactionService(shopGoContext);


            var result = await transactionService.GetTransactions(
                MemberId, Limit, Offset, It.IsAny<string>(), DateFromStr,
                DateToStr, OrderBy, SortDirection
            );

            var transactionResult = result.Data.Single(t => t.TransactionId == transactionId);

            transactionResult.CashbackFlag.Should().Be(expectedCashbackFlag.ToString());
        }

        [Fact]
        public async Task TestGetTransactions_ReturnsSaleDateAest()
        {
            var transactionId = 12345678;
            var saleDateAest = new DateTime(2020, 11, 02, 10, 0, 0);
            var transaction = new TransactionView
            {
                TransactionId = transactionId,
                MemberId = MemberId,
                MerchantId = 123456,
                NetworkId = 123,
                SaleDate = new DateTime(2020, 11, 02),
                TransactionStatus = "Approved",
                TransactionType = (int)TransactionType.Cashback,
                TransactionTypeId = (int)TransactionType.Cashback,
                SaleDateAest = saleDateAest
            };

            var shopGoContext = InitShopGoContext(transaction);
            var transactionService = InitTransactionService(shopGoContext);

            var result = await transactionService.GetTransactions(
                MemberId, Limit, Offset, It.IsAny<string>(), null,
                null, "Date", "Asc"
            );

            var transactionResult = result.Data.Single(t => t.TransactionId == transactionId);
            transactionResult.SaleDateAest.Should().Be(saleDateAest);
        }

        [Fact]
        public async Task TestGetTransactions_SortBySaleDateAest()
        {
            var transactionId = 12345678;
            var saleDateAest = new DateTime(2020, 11, 04, 10, 0, 0);
            var transaction = new TransactionView
            {
                TransactionId = transactionId,
                MemberId = MemberId,
                MerchantId = 123456,
                NetworkId = 123,
                SaleDate = new DateTime(2020, 11, 01),
                TransactionStatus = "Approved",
                TransactionType = (int)TransactionType.Cashback,
                TransactionTypeId = (int)TransactionType.Cashback,
                SaleDateAest = saleDateAest
            };

            var shopGoContext = InitShopGoContext(transaction);
            var transactionService = InitTransactionService(shopGoContext);

            var result = await transactionService.GetTransactions(
                MemberId, Limit, Offset, It.IsAny<string>(), null,
                null, "Date", "Asc"
            );

            DateTime[] sortedDates = { new DateTime(2020, 11, 2), new DateTime(2020, 11, 3), new DateTime(2020, 11, 4), new DateTime(2020, 11, 4), new DateTime(2020, 11, 5) };
            for (int i = 0; i < sortedDates.Length; i++)
            {
                var dateToBeTested = sortedDates[i];
                var transactionItem = result.Data[i];
                transactionItem.SaleDateAest.Date.Should().Be(dateToBeTested);
            }
        }

        [Fact]
        public async Task TestGetTransactions_SearchBySaleDateAest()
        {
            var transactionId = 12345678;
            var saleDateAest = new DateTime(2020, 12, 04, 10, 0, 0);
            var transaction = new TransactionView
            {
                TransactionId = transactionId,
                MemberId = MemberId,
                MerchantId = 123456,
                NetworkId = 123,
                SaleDate = new DateTime(2020, 11, 01),
                TransactionStatus = "Approved",
                TransactionType = (int)TransactionType.Cashback,
                TransactionTypeId = (int)TransactionType.Cashback,
                SaleDateAest = saleDateAest
            };

            var shopGoContext = InitShopGoContext(transaction);
            var transactionService = InitTransactionService(shopGoContext);

            var result = await transactionService.GetTransactions(
                MemberId, Limit, Offset, It.IsAny<string>(), "2020-12-04",
                "2020-12-05", "Date", "Asc"
            );

            result.Data.Count.Should().Be(1);
            result.Data.First().TransactionId.Should().Be(transactionId);
        }

        [Fact]
        public async Task TestGetTransactionsTotalCount()
        {
            var shopGoContext = InitShopGoContext();
            var transactionService = InitTransactionService(shopGoContext);

            //Invalid date from
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactionsTotalCount(
                MemberId, It.IsAny<string>(), FDateFromStr, FDateToStr));

            //Invalid date to
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactionsTotalCount(
                MemberId, It.IsAny<string>(), DateFromStr, FDateToStr));


            //Invalid date from and date to (date from > date to)
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() => transactionService.GetTransactionsTotalCount(
                MemberId, It.IsAny<string>(), DateFromStr,
                "2020-10-31"));

            //Valid
            try
            {
                await transactionService.GetTransactionsTotalCount(MemberId, It.IsAny<string>(), DateFromStr,
                    DateToStr);
            }
            catch (InvalidQueryConditionException ex)
            {
                Assert.True(ex == null);
            }
        }

        private static TransactionService InitTransactionService(IMock<ShopGoContext> shopGoContextMock, Func<IMock<IMemberService>> memberServiceMockFunc = null, Func<IMock<IOptions<Settings>>> settingsMockFunc = null)
        {
            var validationService = InitValidationService();
            var memberServiceMock = memberServiceMockFunc?.Invoke() ?? InitMemberServiceMock();

            var settingsMock = settingsMockFunc?.Invoke() ?? InitSettingsMock();

            var transactionService = new TransactionService(shopGoContextMock.Object, validationService.Object, memberServiceMock.Object, settingsMock.Object);

            return transactionService;
        }

        private static Mock<ShopGoContext> InitShopGoContext(TransactionView transaction = null)
        {
            var transactionViews = new List<TransactionView>
            {
                new TransactionView
                {
                    TransactionId = ApprovedTransactionId,
                    MemberId = MemberId,
                    MerchantId = 123456,
                    SaleDate = new DateTime(2020, 11, 02),
                    TransactionStatus = "Approved",
                    MaxStatus = "Active",
                    SaleDateAest = new DateTime(2020, 11, 02)
                },
                new TransactionView
                {
                    TransactionId = DeclinedTransactionId,
                    MemberId = MemberId,
                    MerchantId = 123456,
                    SaleDate = new DateTime(2020, 11, 03),
                    TransactionStatus = "Declined",
                    SaleDateAest = new DateTime(2020, 11, 03)
                },
                new TransactionView
                {
                    TransactionId = PendingTransactionId,
                    MemberId = MemberId,
                    MerchantId = 123456,
                    SaleDate = new DateTime(2020, 11, 04),
                    ApprovalWaitDays = 1400,
                    TransactionStatus = "Pending",
                    MaxStatus = "Pending",
                    SaleDateAest = new DateTime(2020, 11, 04)
                },
                new TransactionView                
                {
                    TransactionId = RAFTransactionId,
                    MemberId = MemberId,
                    MerchantId = Constant.CashRewardsReferAMateMerchantId,
                    SaleDate = new DateTime(2020, 11, 05),
                    ApprovalWaitDays = 90,
                    TransactionStatus = "Pending",
                    SaleDateAest = new DateTime(2020, 11, 05)
                },
                new TransactionView
                {
                    TransactionId = OverdueTransactionId,
                    MemberId = MemberId,
                    MerchantId = 123456,
                    SaleDate = new DateTime(2020, 11, 06),
                    ApprovalWaitDays = 90,
                    TransactionStatus = "Pending",
                    MaxStatus = "Pending",
                    SaleDateAest = new DateTime(2020, 11, 06)
                },
                 new TransactionView
                {
                    TransactionId = OverdueTransactionId,
                    MemberId = MemberId,
                    MerchantId = 1234,
                    SaleDate = new DateTime(2021, 08, 13),
                    ApprovalWaitDays = 90,
                    TransactionStatus = "Pending",
                    MaxStatus = "Pending",
                    SaleDateAest = new DateTime(2020, 11, 13)
                },
            };

            if(transaction != null)
            {
                transactionViews.Add(transaction);
            }

            var merchants = new List<Merchant>
            {
                new Merchant
                {
                    MerchantId = 123456,
                    MerchantName = "dummy"
                },
                new Merchant
                {
                    MerchantId = Constant.CashRewardsReferAMateMerchantId,
                    MerchantName = "RAF"
                },
                new Merchant
                {
                    MerchantId = Constant.CashRewardsBonusMerchantId,
                    MerchantName = "Bonus"
                },
                new Merchant
                {
                    MerchantId = Constant.CashRewardsPromotionalBonusMerchantId,
                    MerchantName = "Bonus"
                },
                new Merchant
                {
                    MerchantId = Constant.CashRewardsWelcomeBonusMerchantId,
                    MerchantName = "Welcome Bonus"
                },
                new Merchant
                {
                    MerchantId = 1234,
                    MerchantName = "test1",
                    IsPremiumDisabled = true
                }
            };

            var optionsBuilder = new DbContextOptionsBuilder<ShopGoContext>();

            var contextMock = new Mock<ShopGoContext>(optionsBuilder.Options);
            contextMock.Setup(p => p.TransactionView).ReturnsDbSet(transactionViews);
            contextMock.Setup(p => p.Merchant).ReturnsDbSet(merchants);


            return contextMock;
        }

        private static Mock<IValidationService> InitValidationService()
        {
            var validationService = new Mock<IValidationService>();

            /* Invalid condition parameter */

            //Invalid limit
            validationService.Setup(v => v.ValidateQueryConditions(FLimit, It.IsAny<int>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    ApiUsed.Transaction))
                .Throws(new InvalidQueryConditionException("Limit"));

            //Invalid offset
            validationService.Setup(v => v.ValidateQueryConditions(Limit, FOffset,
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    ApiUsed.Transaction))
                .Throws(new InvalidQueryConditionException("Offset"));

            //Invalid date from
            validationService.Setup(v => v.ValidateQueryConditions(Limit, Offset,
                    FDateFromStr, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ApiUsed.Transaction))
                .Throws(new InvalidQueryConditionException("Date from"));
            
            validationService.Setup(v => v.ValidateQueryConditionsForTotalCount(
                    FDateFromStr, It.IsAny<string>()))
                .Throws(new InvalidQueryConditionException("Date from"));

            //Invalid date to
            validationService.Setup(v => v.ValidateQueryConditions(Limit, Offset,
                    DateFromStr, FDateToStr, It.IsAny<string>(), It.IsAny<string>(), ApiUsed.Transaction))
                .Throws(new InvalidQueryConditionException("Date to"));

            validationService.Setup(v => v.ValidateQueryConditionsForTotalCount(DateFromStr, FDateToStr))
                .Throws(new InvalidQueryConditionException("Date to"));

            //Invalid date from and date to (date from > date to)
            validationService.Setup(v => v.ValidateQueryConditions(Limit, Offset,
                    DateFromStr, "2020-10-31", It.IsAny<string>(), It.IsAny<string>(), ApiUsed.Transaction))
                .Throws(new InvalidQueryConditionException("Date to"));

            validationService.Setup(v => v.ValidateQueryConditionsForTotalCount(DateFromStr, "2020-10-31"))
                .Throws(new InvalidQueryConditionException("Date to"));

            //Invalid order by
            validationService.Setup(v =>
                    v.ValidateQueryConditions(Limit, Offset, DateFromStr, DateToStr, FOrderBy, It.IsAny<string>(),
                        ApiUsed.Transaction))
                .Throws(new InvalidQueryConditionException("Order by"));

            //Invalid sort direction
            validationService.Setup(v =>
                    v.ValidateQueryConditions(Limit, Offset, DateFromStr, DateToStr, OrderBy, FSortDirection,
                        ApiUsed.Transaction))
                .Throws(new InvalidQueryConditionException("Sort direction"));

            /* Valid condition parameter */
            validationService.Setup(v =>
                v.ValidateQueryConditions(Limit, Offset, DateFromStr, DateToStr, OrderBy, SortDirection,
                    ApiUsed.Transaction));

            /* Invalid email */
            validationService.Setup(v => v.ValidateEmail(FEmail)).Throws(new InvalidEmailException());
            /* Valid email */
            validationService.Setup(v => v.ValidateEmail(Email));

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

            return validationService;
        }

        private static MembershipDetail GetMembershipInfo(int premiumStatus, DateTime? premiumDateJoined = null)
        {
            var membershipItems = new List<MemberShipItem>();
            membershipItems.Add(new MemberShipItem { ClientId = Constant.Clients.CashRewards, MemberId = MemberId, PremiumStatus = premiumStatus });
            if(premiumStatus == 1)
            membershipItems.Add(new MemberShipItem { ClientId = Constant.Clients.ANZ, MemberId = MemberIdANZ, PremiumStatus = premiumStatus, DateJoined = premiumDateJoined });
            return new MembershipDetail
             {          
                 Items = membershipItems
            };
        }

        private static Mock<IMemberService> InitMemberServiceMock(int premiumStatus = 0, DateTime? premiumDateJoined = null)
        {
            var mock = new Mock<IMemberService>();
            mock.Setup(m => m.GetMembershipInfo(MemberId)).Returns(Task.FromResult(GetMembershipInfo(premiumStatus, premiumDateJoined)));
            return mock;
        }

        private static Mock<IOptions<Settings>> InitSettingsMock(Boolean TransactionMemberViewUseDatabaseView = true)
        {
            var settings = new Settings { TransactionMemberViewUseDatabaseView = TransactionMemberViewUseDatabaseView };
            var optionsMock = new Mock<IOptions<Settings>>();

            optionsMock.Setup(s => s.Value).Returns(settings);

            return optionsMock;
        }
    }
}
