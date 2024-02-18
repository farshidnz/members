using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SettingsAPI.Common;
using SettingsAPI.EF;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using SettingsAPI.Tests.Helpers;
using StackExchange.Redis;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class MemberServiceIntegrationTests
    {
        private class TestState : InMemoryTest
        {
            public MemberService MemberService { get; }

            public TestState()
            {
                MemberService = new MemberService(
                    Mock.Of<IOptions<Settings>>(),
                    Context,
                    ReadOnlyContext,
                    Mock.Of<IEncryptionService>(),
                    Mock.Of<IMemberBalanceService>(),
                    Mock.Of<IMobileOptService>(),
                    Mock.Of<IValidationService>(),
                    Mock.Of<IEmailService>(),
                    Mock.Of<ITimeService>(),
                    Mock.Of<IAwsService>(),
                    Mock.Of<IDatabase>(),
                    Mock.Of<IMapper>(),
                    Mock.Of<IEntityAuditService>(),
                    Mock.Of<IFieldAuditService>(),
                    Mock.Of<IFeatureToggleService>());
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetCashrewardsCognitoMember()
        {
            using var state = new TestState();
            var cognitoId = Guid.NewGuid();
            state.ReadOnlyContext.Member.Add(new Member { MemberId = 1001000123, ClientId = Constant.Clients.ANZ, Status = (int)StatusType.Active, ClickWindowActive = true, PopUpActive = true, ReceiveNewsLetter = true, RequiredLogin = true, RowVersion = Encoding.ASCII.GetBytes("1"), SaltKey = "salt" });
            state.ReadOnlyContext.Member.Add(new Member { MemberId = 1001000124, ClientId = Constant.Clients.CashRewards, Status = (int)StatusType.Active, ClickWindowActive = true, PopUpActive = true, ReceiveNewsLetter = true, RequiredLogin = true, RowVersion = Encoding.ASCII.GetBytes("1"), SaltKey = "salt" });
            state.ReadOnlyContext.Member.Add(new Member { MemberId = 1001000125, ClientId = Constant.Clients.MoneyMe, Status = (int)StatusType.Active, ClickWindowActive = true, PopUpActive = true, ReceiveNewsLetter = true, RequiredLogin = true, RowVersion = Encoding.ASCII.GetBytes("1"), SaltKey = "salt" });
            state.ReadOnlyContext.CognitoMember.Add(new CognitoMember { MemberId = 1001000123, CognitoId = cognitoId.ToString(), Status = true });
            state.ReadOnlyContext.CognitoMember.Add(new CognitoMember { MemberId = 1001000124, CognitoId = cognitoId.ToString(), Status = true });
            state.ReadOnlyContext.CognitoMember.Add(new CognitoMember { MemberId = 1001000125, CognitoId = cognitoId.ToString(), Status = true });
            state.ReadOnlyContext.SaveChanges();

            var result = await state.MemberService.GetCashrewardsCognitoMember(cognitoId.ToString());

            result.Should().BeEquivalentTo(new CognitoMember
            {
                CognitoId = cognitoId.ToString(),
                MemberId = 1001000124,
                MappingId = 2,
                Status = true,
                Member = new Member
                {
                    ClientId = Constant.Clients.CashRewards,
                    MemberId = 1001000124,
                    MemberNewId = Guid.Empty,
                    Status = 1, 
                    ClickWindowActive = true, 
                    PopUpActive = true, 
                    ReceiveNewsLetter = true, 
                    RequiredLogin = true, 
                    RowVersion = Encoding.ASCII.GetBytes("1"), 
                    SaltKey = "salt"
                }
            });
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetMember_ShouldReturnMember_GivenNoPersonRecordExists()
        {
            using var state = new TestState();
            state.ReadOnlyContext.Member.Add(new Member { MemberId = 1001000123, Status = (int)StatusType.Active, ClickWindowActive = true, PopUpActive = true, ReceiveNewsLetter = true, RequiredLogin = true, RowVersion = Encoding.ASCII.GetBytes("1"), SaltKey = "salt" });
            state.ReadOnlyContext.CognitoMember.Add(new CognitoMember { MemberId = 1001000123 });
            state.ReadOnlyContext.SaveChanges();

            var result = await state.MemberService.GetMember(299000, 1001000123);

            result.Should().BeEquivalentTo(new MemberDetails
            {
                MemberId = 1001000123,
                NewMemberId = Guid.Empty.ToString(),
                Balance = 0,
                AvailableBalance = 0,
                RedeemBalance = 0,
                ShowCommunicationsPrompt = true,
                ReceiveNewsLetter = true,
            });
        }
    }
}
