using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SettingsAPI.Controllers;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests.Controller
{

    public class TestMemberController
    {
        private Mock<IMemberService> _mockMemberService;
        private Mock<IHttpContextAccessor> _httpContextAccessor;        


        private const int _memberId = 123456;

        public MemberController InitMemberController()
        {
            _mockMemberService = new Mock<IMemberService>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>() { new Claim("memberId", _memberId.ToString()) }));

            _httpContextAccessor.Setup(h => h.HttpContext).Returns(context);

            return new MemberController(_mockMemberService.Object, new Mock<ITransactionService>().Object,
                new Mock<IMemberBankAccountService>().Object, _httpContextAccessor.Object,
                new Mock<IMemberClicksHistoryService>().Object, new Mock<IMemberRedeemService>().Object,
                new Mock<IMemberPaypalAccountService>().Object, new Mock<IValidationService>().Object,
                new Mock<IMemberFavouriteService>().Object,
                new Mock<IMemberFavouriteCategoryService>().Object,
                new Mock<IRequestContext>().Object,
                new Mock<IMapper>().Object);
        }


        [Theory]
        [InlineData("Close")]
        [InlineData("Review")]
        public async Task CommunicationsPopupShown_ShouldParseActionEnum_AndPassToMemberService(string actionString)
        {
            var memberController = InitMemberController();

            var result = await memberController.CommunicationsPromptShown(new CommsPromptShownRequest() { Action = actionString});
            var value = (result.Result as ObjectResult).Value as ApiMessageResponse;

            _mockMemberService.Verify(s => s.CommsPromptShown(It.IsAny<CommsPromptShownModel>()), Times.Once);

            value.Should().NotBeNull();
            value.Code.Should().Be(200);
            value.Status.Should().Be("UPDATED");


        }

        [Fact]
        public async Task CommunicationsPopupShown_ShouldReturnBadRequest_ForInvalidAction()
        {
            var memberController = InitMemberController();

            var result = await memberController.CommunicationsPromptShown(new CommsPromptShownRequest() { Action = "invalid" });
            var value = (result.Result as ObjectResult).Value as ApiMessageResponse;

            var model = new CommsPromptShownModel
            {
                PersonId = It.IsAny<int>(),
                MemberId = It.IsAny<int>(),
                Action = It.IsAny<CommsPromptDismissalAction>()
            };
            _mockMemberService.Verify(s => s.CommsPromptShown(model), Times.Never);

            value.Should().NotBeNull();
            value.Code.Should().Be(400);
            value.Status.Should().Be("INVALID_REQUEST");
            value.Message.Should().Be("Bad request");
        }

        [Fact]
        public async Task UpdateCommsPreferences_ShouldCallMemberServiceWithNewsletterSMSAndAppPushNotificationValues()
        {
            var memberController = InitMemberController();
            var request = new CommsPreferencesRequest()
            {
                SubscribeNewsletters = true,
                SubscribeSMS = null,
                SubscribeAppNotifications = false
            };

            await memberController.UpdateCommsPreferences(request);
            _mockMemberService.Verify(s => s.UpdateCommsPreferences(It.IsAny<UpdateCommsPreferencesModel>()), Times.Once);
        }

        [Fact]
        public async Task GetMaskedMobile_ShouldCallMemberServiceWithMaskedMobileNumber()
        {
            var memberController = InitMemberController();
            
            var maskedMobile = await memberController.GetMaskedMobile();
            _mockMemberService.Verify(s => s.GetMaskedMobileNumber(It.Is<int>(s => s == _memberId)), Times.Once);
        }

        [Fact]
        public async Task GetHashedEmail_ShouldCallMemberServiceWithHashedEmail()
        {
            var memberController = InitMemberController();

            var hashedEmail = await memberController.GetHashedEmail();
            _mockMemberService.Verify(s => s.GetHashedSurveyEmail(It.Is<int>(s => s == _memberId)), Times.Once);
        }
    }
}
