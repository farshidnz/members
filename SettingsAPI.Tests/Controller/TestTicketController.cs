using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SettingsAPI.Common;
using SettingsAPI.Controllers;
using SettingsAPI.EF;
using SettingsAPI.Model.Rest;
using SettingsAPI.Model.Rest.CreateTicket;
using SettingsAPI.Service;
using Xunit;

namespace SettingsAPI.Tests.Controller
{
   
    public class TestTicketController
    {
        private const int CognitoId = 1234;
        private const int MemberId = 1234;
        private const int PersonId = 5678;

        private readonly Mock<IFreshdeskService> _mockFreshdeskService;
        private readonly Mock<IMemberService> _mockMemberService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;     

        public TestTicketController()
        {
            _mockFreshdeskService = new Mock<IFreshdeskService>();
            _mockMemberService = new Mock<IMemberService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>() { new Claim("cognitoId", CognitoId.ToString()) }))
            };
            _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(context);
            _mockMemberService.Setup(m => m.GetCashrewardsCognitoMember(CognitoId.ToString()))
                .Returns(Task.FromResult(new CognitoMember
                {
                    CognitoId = CognitoId.ToString(),
                    MemberId = MemberId,
                    PersonId = PersonId
                }));
        }
        

        [Fact]
        public async Task TestCreateTicketError()
        {
            var createTicketRequest = new CreateTicketRequest();
            _mockFreshdeskService.Setup(s => s.CreateTicket(MemberId, PersonId, createTicketRequest))
                .Returns(Task.FromResult(false));
           
            var ticketController = new TicketController(_mockFreshdeskService.Object, _mockHttpContextAccessor.Object, _mockMemberService.Object);
            
            var result = await ticketController.CreateTicket(createTicketRequest);
            
            var value = (result.Result as ObjectResult)?.Value as ApiMessageResponse;
           
            _mockFreshdeskService.Verify(s => s.CreateTicket(MemberId, PersonId, createTicketRequest), Times.Once);
            
            value.Should().NotBeNull();
            value?.Code.Should().Be(HttpStatusCode.InternalServerError.GetHashCode());
            value?.Status.Should().Be(AppMessage.ApiResponseStatusErrorInternal);
            
        }
        
        [Fact]
        public async Task TestCreateTicketSuccess()
        {

            var createTicketRequest = new CreateTicketRequest
            {
                Contact = "abc@gmail.com",
                FirstName = "abc",
                LastName = "abc",
                DateOfPurchase = "2021-05-05",
                Store = "Shopee",
                SaleValueTracked = "123",
                Cashback = "123",
                EstimateApprovalTimeframe = "100 days",
                SaleValueExcepted = "50",
                CashbackOptional = "50",
                OrderId = "abc",
                PremiumMember = "Yes",
                EnquiryReason = "Other",
                IsApprovalDatePass = "Yes",
                Info = "test",
                AdditionalInformation = "test",
                TransferDurationPassed = "No"
            };
            _mockFreshdeskService.Setup(s => s.CreateTicket(MemberId, PersonId, createTicketRequest))
                .Returns(Task.FromResult(true));
            
            var ticketController = new TicketController(_mockFreshdeskService.Object, _mockHttpContextAccessor.Object, _mockMemberService.Object);
            
            var result = await ticketController.CreateTicket(createTicketRequest);
            
            var value = (result.Result as ObjectResult)?.Value as ApiMessageResponse;
           
            _mockFreshdeskService.Verify(s => s.CreateTicket(MemberId, PersonId, createTicketRequest), Times.Once);

            value.Should().NotBeNull();
            value?.Code.Should().Be(HttpStatusCode.Created.GetHashCode());
            value?.Status.Should().Be(AppMessage.ApiResponseStatusCreated);
        }

    }
}
