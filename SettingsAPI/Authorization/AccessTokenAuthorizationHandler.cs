using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SettingsAPI.Common;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Authorization
{
    public class AccessTokenRequirement : IAuthorizationRequirement
    {
        public AccessTokenRequirement()
        {
        }
    }

    public class AccessTokenAuthorizationHandler : AuthorizationHandler<AccessTokenRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        private readonly ITokenValidation tokenValidation;

        public AccessTokenAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ITokenValidation tokenValidation)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.tokenValidation = tokenValidation;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessTokenRequirement requirement)
        {
            try
            {
                var accessToken = httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization];
                accessToken = accessToken.ToString().Replace("Bearer", string.Empty).Trim();

                var validatedToken = tokenValidation.ValidateToken(accessToken);
                validatedToken.Payload.TryGetValue("username", out var cognitoId);
                validatedToken.Payload.TryGetValue("sub", out var memberId);

                var claims = new List<Claim>();
                if (cognitoId != null)
                {
                    claims.Add(new Claim(Constant.CognitoIdClaimPropertyName, cognitoId as string));
                }
                else if (memberId != null)
                {
                    claims.Add(new Claim(Constant.MemberIdClaimPropertyName, memberId as string));
                }
                var appIdentity = new ClaimsIdentity(claims);

                context.User.AddIdentity(appIdentity);

                context.Succeed(requirement);
            } 
            catch
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }


    }
}
