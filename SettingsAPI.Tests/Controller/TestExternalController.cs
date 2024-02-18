using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using SettingsAPI.Controllers;
using SettingsAPI.Model.Rest;
using SettingsAPI.Service;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests.Controller
{
   
    public class TestExternalController
    {
        private const int memberId = 1234;

        Mock<IOptions<Settings>> options;
        Mock<IMemberService> memberService;

        public TestExternalController()
        {
            options = new Mock<IOptions<Settings>>();
            memberService = new Mock<IMemberService>();
        }

        public ExternalController CreateController()
        {
            return new ExternalController(options.Object, memberService.Object);
        }

        [Fact]
        public async Task SmsOptOut_ValidatesApiKey()
        {
            var correctKey = "sdlashdiashdkajshdaksjdhaksj";
            var incorrectKey = "8372guwydgf2873gr2wieurfg";

            options.Setup(o => o.Value).Returns(new Settings()
            {
                OptimiseSmsOptOutKey = correctKey
            });

            var controller = CreateController();

            var request = new SmsOptOutRequest()
            {
                Key = incorrectKey,
                MemberId = memberId
            };

            var result = (await controller.SmsOptOut(request)).Result as ObjectResult;
            result.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            var value = result.Value as ApiMessageResponse;
            value.Code.Should().Be(401);
            value.Status.Should().Be("UNAUTHORIZED");
            value.Message.Should().Be("Incorrect API key");

            request.Key = correctKey;

            result = (await controller.SmsOptOut(request)).Result as ObjectResult;
            result.StatusCode.Should().Be((int)HttpStatusCode.OK);
            value = result.Value as ApiMessageResponse;
            value.Code.Should().Be(200);
            value.Status.Should().Be("UPDATED");
            value.Message.Should().Be("Member was opted out from SMS");

        }

        [Fact]
        public async Task SmsOptOut_UpdatesCommsPreferencesForMember()
        {
            var key = "sdlashdiashdkajshdaksjdhaksj";

            options.Setup(o => o.Value).Returns(new Settings()
            {
                OptimiseSmsOptOutKey = key
            });

            var controller = CreateController();

            var request = new SmsOptOutRequest()
            {
                Key = key,
                MemberId = memberId
            };

            var result = (await controller.SmsOptOut(request)).Result as ObjectResult;
            memberService.Verify(m => m.UpdateCommsPreferences(It.IsAny<UpdateCommsPreferencesModel>()));

            result.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var value = result.Value as ApiMessageResponse;
            value.Code.Should().Be(200);
            value.Status.Should().Be("UPDATED");
            value.Message.Should().Be("Member was opted out from SMS");

        }


    }
}
