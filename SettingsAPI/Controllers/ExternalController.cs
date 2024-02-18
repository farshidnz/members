using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SettingsAPI.Common;
using SettingsAPI.Error;
using SettingsAPI.Model.Rest;
using SettingsAPI.Service;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalController : ControllerBase
    {
        private readonly IOptions<Settings> _settings;
        private readonly IMemberService _memberService;

        public ExternalController(IOptions<Settings> settings, IMemberService memberService)
        {
            this._settings = settings;
            this._memberService = memberService;
        }

        [HttpPost("sms-opt-out")]
        public async Task<ActionResult<ApiMessageResponse>> SmsOptOut([FromBody] SmsOptOutRequest smsOptOutRequest)
        {
            var apiResponse = new ApiMessageResponse();

            if (smsOptOutRequest.Key != _settings.Value.OptimiseSmsOptOutKey)
            {
                apiResponse.Code = HttpStatusCode.Unauthorized.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusUnauthorized;
                apiResponse.Message = "Incorrect API key";

                return Unauthorized(apiResponse);
            }

            try
            {
                await _memberService.UpdateCommsPreferences(new UpdateCommsPreferencesModel
                {
                    MemberId = smsOptOutRequest.MemberId,
                    SubscribeSMS = false
                });
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }

            apiResponse.Code = HttpStatusCode.OK.GetHashCode();
            apiResponse.Status = AppMessage.ApiResponseStatusUpdated;
            apiResponse.Message = "Member was opted out from SMS";

            return Ok(apiResponse);
        }
    }
}
