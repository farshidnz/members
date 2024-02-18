using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

namespace SettingsAPI.Tests.Service
{
    public class TestMemberClickHistoryService
    {
        /* Common init data */
        private const int MemberId = 10000;
        private const int MerchantId = 10000;

        /* Valid data (true) */
        private const int Limit = 10;
        private const int Offset = 0;
        private const string DateFromStr = "2020-11-01";
        private const string DateToStr = "2020-11-02";
        private const string OrderBy = "Date";
        private const string SortDirection = "Asc";
        private const int PremiumStatus = 1;
        private DateTime PremiumDateJoined = DateTime.Parse("2021/08/12");


        /* Invalid data (false) */

        private const int FLimit = 0;
        private const int FOffset = -1;
        private const string FDateFromStr = "2020-11/01";
        private const string FDateToStr = "2020-11/02";
        private const string FOrderBy = "dummy";
        private const string FSortDirection = "dummy";
        private const int FPremiumStatus = 0;


        [Fact]
        public async Task TestGetMemberClicksHistory()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberServiceClickHistories = InitMemberClicksHistoryService(shopGoContext, readOnlyShopGoContext);

            /* Invalid query condition */

            //Invalid limit
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistory(
                    It.IsAny<int>(), FLimit, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            //Invalid offset
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistory(
                    It.IsAny<int>(), Limit, FOffset, It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            //Invalid date from
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistory(
                    It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), FDateFromStr,
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            //Invalid date to
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistory(
                    It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                    FDateToStr, It.IsAny<string>(), It.IsAny<string>()));

            //Invalid date from and date to (date from > date to)
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistory(
                    It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                    "2020-10-31", It.IsAny<string>(), It.IsAny<string>()));

            //Invalid order by
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistory(
                    It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                    DateToStr, FOrderBy, It.IsAny<string>()));

            //Invalid sort direction
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistory(
                    It.IsAny<int>(), Limit, Offset, It.IsAny<string>(), DateFromStr,
                    DateToStr, OrderBy, FSortDirection));

            //Valid
            try
            {
                await memberServiceClickHistories.GetMemberClicksHistory(MemberId, Limit, Offset, It.IsAny<string>(),
                    DateFromStr, DateToStr, OrderBy, SortDirection);
            }
            catch (InvalidQueryConditionException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public async Task TestGetMemberClicksHistoryForPremiumMember()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberServiceClickHistories = InitMemberClicksHistoryService(shopGoContext, readOnlyShopGoContext, () => InitMemberServiceMock(PremiumStatus, PremiumDateJoined));


            var result = await memberServiceClickHistories.GetMemberClicksHistory(MemberId, Limit, Offset, It.IsAny<string>(),
                null, null, OrderBy, SortDirection);

            var clickHistory = result.Data;

            clickHistory.Count.Should().Be(2);
        }

        [Fact]
        public async Task TestGetMemberClicksHistoryForNonPremiumMember()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberServiceClickHistories = InitMemberClicksHistoryService(shopGoContext, readOnlyShopGoContext);


            var result = await memberServiceClickHistories.GetMemberClicksHistory(MemberId, Limit, Offset, It.IsAny<string>(),
                null, null, OrderBy, SortDirection);

            var clickHistory = result.Data;

            clickHistory.Count.Should().Be(3);
        }


        [Fact]
        public async Task TestGetMemberClicksHistoryTotalCount()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberServiceClickHistories = InitMemberClicksHistoryService(shopGoContext, readOnlyShopGoContext);

            /* Invalid query condition */

            //Invalid date from
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistoryTotalCount(
                    It.IsAny<int>(), It.IsAny<string>(), FDateFromStr, It.IsAny<string>()));

            //Invalid date to
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistoryTotalCount(It.IsAny<int>(), It.IsAny<string>(),
                    DateFromStr, FDateToStr));

            //Invalid date from and date to (date from > date to)
            await Assert.ThrowsAsync<InvalidQueryConditionException>(() =>
                memberServiceClickHistories.GetMemberClicksHistoryTotalCount(It.IsAny<int>(), It.IsAny<string>(),
                    DateFromStr, "2020-10-31"));

            //Valid
            try
            {
                await memberServiceClickHistories.GetMemberClicksHistoryTotalCount(MemberId, It.IsAny<string>(),
                    DateFromStr, DateToStr);
            }
            catch (InvalidQueryConditionException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public async Task TestClicksHistoryTotalCountForPremiumMember()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberServiceClickHistories = InitMemberClicksHistoryService(shopGoContext, readOnlyShopGoContext, () => InitMemberServiceMock(PremiumStatus, PremiumDateJoined));

            var result = await memberServiceClickHistories.GetMemberClicksHistoryTotalCount(MemberId, It.IsAny<string>(),
                null, null);

            var count = result.TotalCount;

            count.Should().Be(2);
        }

        [Fact]
        public async Task TestClicksHistoryTotalCountForNonPremiumMember()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberServiceClickHistories = InitMemberClicksHistoryService(shopGoContext, readOnlyShopGoContext);

            var result = await memberServiceClickHistories.GetMemberClicksHistoryTotalCount(MemberId, It.IsAny<string>(),
                null, null);

            var count = result.TotalCount;

            count.Should().Be(3);
        }

        private static MemberClicksHistoryService InitMemberClicksHistoryService(IMock<ShopGoContext> shopGoContextMock, IMock<ReadOnlyShopGoContext> readOnlyShopGoContextMock, Func<IMock<IMemberService>> memberServiceMockFunc = null)
        {
            var validationService = InitValidationService();
            var memberServiceMock = memberServiceMockFunc?.Invoke() ?? InitMemberServiceMock();

            var service = new MemberClicksHistoryService(shopGoContextMock.Object, readOnlyShopGoContextMock.Object,
                validationService.Object, memberServiceMock.Object);

            return service;
        }

        private static Mock<IValidationService> InitValidationService()
        {
            var validationService = new Mock<IValidationService>();

            /* Invalid condition parameter */

            //Invalid limit
            validationService.Setup(v => v.ValidateQueryConditions(FLimit, It.IsAny<int>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    ApiUsed.MemberClickHistory))
                .Throws(new InvalidQueryConditionException("Limit"));

            //Invalid offset
            validationService.Setup(v => v.ValidateQueryConditions(Limit, FOffset,
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    ApiUsed.MemberClickHistory))
                .Throws(new InvalidQueryConditionException("Offset"));

            //Invalid date from
            validationService.Setup(v => v.ValidateQueryConditions(Limit, Offset,
                    FDateFromStr, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ApiUsed.MemberClickHistory))
                .Throws(new InvalidQueryConditionException("Date from"));

            validationService.Setup(v => v.ValidateQueryConditionsForTotalCount(FDateFromStr, It.IsAny<string>()))
                .Throws(new InvalidQueryConditionException("Date from"));

            //Invalid date to
            validationService.Setup(v => v.ValidateQueryConditions(Limit, Offset,
                    DateFromStr, FDateToStr, It.IsAny<string>(), It.IsAny<string>(), ApiUsed.MemberClickHistory))
                .Throws(new InvalidQueryConditionException("Date to"));

            validationService.Setup(v => v.ValidateQueryConditionsForTotalCount(DateFromStr, FDateToStr))
                .Throws(new InvalidQueryConditionException("Date to"));

            //Invalid date from and date to (date from > date to)
            validationService.Setup(v => v.ValidateQueryConditions(Limit, Offset,
                    DateFromStr, "2020-10-31", It.IsAny<string>(), It.IsAny<string>(), ApiUsed.MemberClickHistory))
                .Throws(new InvalidQueryConditionException("Date to"));

            validationService.Setup(v => v.ValidateQueryConditionsForTotalCount(DateFromStr, "2020-10-31"))
                .Throws(new InvalidQueryConditionException("Date to"));

            //Invalid order by
            validationService.Setup(p =>
                    p.ValidateQueryConditions(Limit, Offset, DateFromStr, DateToStr, FOrderBy, It.IsAny<string>(),
                        ApiUsed.MemberClickHistory))
                .Throws(new InvalidQueryConditionException("Order by"));

            //Invalid sort direction
            validationService.Setup(p =>
                    p.ValidateQueryConditions(Limit, Offset, DateFromStr, DateToStr, OrderBy, FSortDirection,
                        ApiUsed.MemberClickHistory))
                .Throws(new InvalidQueryConditionException("Sort direction"));

            /* Valid condition parameter */
            validationService.Setup(p =>
                p.ValidateQueryConditions(Limit, Offset, DateFromStr, DateToStr, OrderBy, SortDirection,
                    ApiUsed.MemberClickHistory));


            return validationService;
        }

        private static List<MemberClicks> InitClickHistories()
        {
            var clickHistories = new List<MemberClicks>
            {
                new MemberClicks
                {
                    MemberId = MemberId,
                    DateCreated = DateTime.Now,
                    MerchantId = MerchantId
                },
                new MemberClicks
                {
                    MemberId = MemberId,
                    DateCreated = DateTime.Now,
                    MerchantId = 1234
                },
                 new MemberClicks
                {
                    MemberId = MemberId,
                    DateCreated = DateTime.Parse("2020/06/12"),
                    MerchantId = MerchantId
                }
            };
            return clickHistories;
        }

        private static List<Merchant> InitMerchants()
        {

            var merchants = new List<Merchant>
            {
                new Merchant
                {
                    MerchantId = MerchantId,
                    MerchantName = "dummy"
                },
                new Merchant
                {
                    MerchantId = 1234,
                    MerchantName = "dummy1",
                    IsPremiumDisabled = true
                }
            };
            return merchants;
        }

        private static Mock<ShopGoContext> InitShopGoContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ShopGoContext>();
            var contextMock = new Mock<ShopGoContext>(optionsBuilder.Options);

            contextMock.Setup(c => c.MemberClicks).ReturnsDbSet(InitClickHistories());

            contextMock.Setup(c => c.Merchant).ReturnsDbSet(InitMerchants());

            return contextMock;
        }
        private static Mock<ReadOnlyShopGoContext> InitReadOnlyShopGoContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ReadOnlyShopGoContext>();
            var contextMock = new Mock<ReadOnlyShopGoContext>(optionsBuilder.Options);

            contextMock.Setup(c => c.MemberClicks).ReturnsDbSet(InitClickHistories());

            contextMock.Setup(c => c.Merchant).ReturnsDbSet(InitMerchants());

            return contextMock;
        }
        private static MembershipDetail GetMembershipInfo(int premiumStatus, DateTime? premiumDateJoined = null)
        {
            var membershipItems = new List<MemberShipItem>();
            membershipItems.Add(new MemberShipItem { ClientId = Constant.Clients.CashRewards, MemberId = MemberId, PremiumStatus = premiumStatus });
            if (premiumStatus == 1)
                membershipItems.Add(new MemberShipItem { ClientId = Constant.Clients.ANZ, MemberId = MemberId, PremiumStatus = premiumStatus, DateJoined = premiumDateJoined });
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
    }
}