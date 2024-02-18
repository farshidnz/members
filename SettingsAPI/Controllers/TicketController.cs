using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SettingsAPI.Common;
using SettingsAPI.Model.Rest;
using SettingsAPI.Model.Rest.CreateTicket;
using SettingsAPI.Service;

namespace SettingsAPI.Controllers
{
    [EnableCors]
    [Authorize]
    public class TicketController : BaseController
    {
        private readonly IFreshdeskService _freshdeskService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberService _memberService;

        public TicketController(IFreshdeskService freshdeskService, IHttpContextAccessor httpContextAccessor,
            IMemberService memberService)
        {
            _freshdeskService = freshdeskService;
            _httpContextAccessor = httpContextAccessor;
            _memberService = memberService;
        }

        [HttpOptions]
        public IActionResult PreflightRoute()
        {
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<ApiMessageResponse>> CreateTicket([FromForm] CreateTicketRequest ticketRequest)
        {
                var apiResponse = new ApiMessageResponse();
                var (personId, memberId) = await GetPersonIdAndMemberIdFromContext();
                var isCreated = await _freshdeskService.CreateTicket(memberId, personId, ticketRequest);

                if (isCreated)
                {
                    apiResponse.Code = HttpStatusCode.Created.GetHashCode();
                    apiResponse.Status = AppMessage.ApiResponseStatusCreated;
                    apiResponse.Message = AppMessage.CreateTicketSuccessful;

                    return StatusCode(HttpStatusCode.Created.GetHashCode(), apiResponse);
                }
                
                apiResponse.Code = HttpStatusCode.InternalServerError.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusErrorInternal);
                apiResponse.Message = AppMessage.CreateTicketError;

                return StatusCode(HttpStatusCode.InternalServerError.GetHashCode(), apiResponse);
                
        }

        private async Task<(int?, int)> GetPersonIdAndMemberIdFromContext()
        {
            var cp = _httpContextAccessor.HttpContext.User;
            var cognitoIdClaim = cp.FindFirst(Constant.CognitoIdClaimPropertyName);
            if (cognitoIdClaim == null)
                return (null, int.Parse(cp.FindFirst(Constant.MemberIdClaimPropertyName)?.Value ?? string.Empty));

            var cognitoMember = await _memberService.GetCashrewardsCognitoMember(cognitoIdClaim.Value);
            return (cognitoMember.PersonId, cognitoMember.MemberId);
        }
    }
}