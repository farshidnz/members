using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SettingsAPI.Common;
using SettingsAPI.Service.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class RequestContext : IRequestContext
    {
        private readonly IMemberService _memberService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestContext(IHttpContextAccessor httpContextAccessor
            , IMemberService memberService)
        {
            _httpContextAccessor = httpContextAccessor;
            _memberService = memberService;
        }

        private string _cognitoId;

        private ClaimsPrincipal Claims => _httpContextAccessor.HttpContext.User;

        public string CognitoId
        {
            get
            {
                if (string.IsNullOrEmpty(_cognitoId))
                {
                    ClaimsPrincipal cp = _httpContextAccessor.HttpContext.User;
                    var cognitoIdClaim = cp.FindFirst(Constant.CognitoIdClaimPropertyName);
#if DEBUG
                    var token = new JwtSecurityToken(_httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer", string.Empty).Trim());
                    cognitoIdClaim = token.Claims.FirstOrDefault(c => c.Type == "username");
#endif

                    _cognitoId = cognitoIdClaim?.Value;
                }
                return _cognitoId;
            }
        }

        public async Task<int> GetMemberIdFromContext()
        {
            if (!string.IsNullOrEmpty(CognitoId))
            {
                var cognitoMember = await _memberService.GetCashrewardsCognitoMember(CognitoId);
                return cognitoMember.MemberId;
            }

            return int.Parse(Claims.FindFirst(Constant.MemberIdClaimPropertyName)?.Value);
        }

        public async Task<(int?, int)> GetPersonIdAndMemberIdFromContext()
        {
            var cognitoIdClaim = Claims.FindFirst(Constant.CognitoIdClaimPropertyName);

#if DEBUG
            var token = new JwtSecurityToken(_httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer", string.Empty).Trim());
            cognitoIdClaim = token.Claims.FirstOrDefault(c => c.Type == "username");
#endif

            if (cognitoIdClaim != null)
            {
                var cognitoMember = await _memberService.GetCashrewardsCognitoMember(cognitoIdClaim.Value);
                return (cognitoMember.PersonId, cognitoMember.MemberId);
            }

            return (null, int.Parse(Claims.FindFirst(Constant.MemberIdClaimPropertyName)?.Value));
        }
    }
}