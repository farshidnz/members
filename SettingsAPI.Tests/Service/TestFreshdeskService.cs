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
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class TestFreshdeskService
    {
        
        [Fact]
        public async Task TestCreateTicketCalled()
        {
            var ticketHelperServiceMock = new Mock<IFreshdeskTicketHelperService>();
            var optionSettingMock =  new Mock<IOptions<Settings>>();
            var contextMock = new Mock<ShopGoContext>(new DbContextOptionsBuilder<ShopGoContext>().Options);
            
            contextMock.Setup(context => context.Member).ReturnsDbSet(new List<Member>
            {
                new Member
                {
                    MemberId = 1
                }
            });
            
            contextMock.Setup(context => context.Person).ReturnsDbSet(new List<Person>
            {
                new Person
                {
                    PersonId = 1,
                    PremiumStatus = 0
                }
            });

            const string freshdeskApiKey = "dummy";
            const string  freshdeskDomain = "dummy";
            
            optionSettingMock.Setup(x => x.Value).Returns(new Settings
            {
                FreshdeskApiKey = freshdeskApiKey,
                FreshdeskDomain = freshdeskDomain
            });

            var freshdeskService = new FreshdeskService(optionSettingMock.Object, ticketHelperServiceMock.Object,
                contextMock.Object);

            await freshdeskService.CreateTicket(1, 1, It.IsAny<CreateTicketRequest>());
            
            ticketHelperServiceMock.Verify(
                helper => helper.CreateFreshDeskTicket(
                    freshdeskApiKey,
                    freshdeskDomain, 
                    1,
                    It.IsAny<CreateTicketRequest>(),
                    0
                    ), Times.Once
                );
        }
    }
}