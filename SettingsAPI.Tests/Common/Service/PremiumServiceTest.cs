using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Model.Enum;
using SettingsAPI.Service;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests.Common.Service
{
    public class PremiumServiceTests
    {
        private class TestState
        {
            public PremiumService PremiumService { get; }
            public Person NotEnrolledPerson { get; } = new Person { PremiumStatus = (int)PremiumStatusEnum.NotEnrolled };

            public Person EnrolledPerson { get; }

            public Person OptOutPerson { get; }

            public TestState()
            {
                var options = new DbContextOptionsBuilder<ReadOnlyShopGoContext>()
                .UseInMemoryDatabase(databaseName: "ShopGo")
                .Options;
                ReadOnlyShopGoContext shopgoDb = new ReadOnlyShopGoContext(options);

                Person NotEnrolledPerson = new Person
                {
                    CognitoId = Guid.NewGuid(),
                    PremiumStatus = (int)PremiumStatusEnum.NotEnrolled
                };

                EnrolledPerson = new Person
                {
                    CognitoId = Guid.NewGuid(),
                    PremiumStatus = (int)PremiumStatusEnum.Enrolled,
                };

                OptOutPerson = new Person
                {
                    CognitoId = Guid.NewGuid(),
                    PremiumStatus = (int)PremiumStatusEnum.OptOut,
                };

                shopgoDb.Person.Add(NotEnrolledPerson);
                shopgoDb.Person.Add(EnrolledPerson);
                shopgoDb.Person.Add(OptOutPerson);
                shopgoDb.SaveChanges();

                PremiumService = new PremiumService(shopgoDb);
            }
        }

        [Fact]
        public async Task GetPremiumMembership_ShouldReturnNull_GivenNullOrEmptyCognitoId()
        {
            var state = new TestState();

            var premium = await state.PremiumService.GetPremiumMembership(Constant.Clients.CashRewards, string.Empty);

            premium.Should().BeNull();
        }

        [Fact]
        public async Task GetPremiumMembership_ShouldReturnNull_GivenNoPersonExists()
        {
            var state = new TestState();

            var premium = await state.PremiumService.GetPremiumMembership(Constant.Clients.CashRewards, Guid.NewGuid().ToString());

            premium.Should().BeNull();
        }

        [Fact]
        public async Task GetPremiumMembership_ShouldReturnNull_GivenPersonIsNotEnrolled()
        {
            var state = new TestState();

            var premium = await state.PremiumService.GetPremiumMembership(Constant.Clients.CashRewards, state.NotEnrolledPerson.CognitoId.ToString());

            premium.Should().BeNull();
        }

        [Fact]
        public async Task GetPremiumMembership_ShouldReturnNull_GivenNonCashrewardsClientId()
        {
            var state = new TestState();

            var premium = await state.PremiumService.GetPremiumMembership(Constant.Clients.MoneyMe, state.EnrolledPerson.CognitoId.ToString());

            premium.Should().BeNull();
        }

        [Fact(Skip = "ANZ Premium client id has been removed")]
        public async Task GetPremiumMembership_ShouldReturnPremiumMembership_GivenAnEnrolledPersion()
        {
            var state = new TestState();

            var premium = await state.PremiumService.GetPremiumMembership(Constant.Clients.CashRewards, state.EnrolledPerson.CognitoId.ToString());

            premium.Should().BeEquivalentTo(new PremiumMembership
            {
                PremiumClientId = Constant.Clients.ANZ,
                IsCurrentlyActive = true
            });
        }

        [Fact(Skip = "ANZ Premium client id has been removed")]
        public async Task GetPremiumMembership_ShouldReturnInactivePremiumMembership_GivenAnOptOutPersion()
        {
            var state = new TestState();

            var premium = await state.PremiumService.GetPremiumMembership(Constant.Clients.CashRewards, state.OptOutPerson.CognitoId.ToString());

            premium.Should().BeEquivalentTo(new PremiumMembership
            {
                PremiumClientId = Constant.Clients.ANZ,
                IsCurrentlyActive = false
            });
        }
    }
}