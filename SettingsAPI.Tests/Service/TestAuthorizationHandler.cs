using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Moq;
using SettingsAPI.Authorization;
using SettingsAPI.Common;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class TestAuthorizationHandler
    {
        private class TestState
        {
            public TestState(string stsKeys = validStsKeys, string cognitoKeys = validCognitoKeys)
            {
                var settings = Options.Create(new Settings()
                {
                    StsTokenIssuerEndpoint = "https://test-sts.cashrewards.com.au/stg/.well-known/jwks",
                    CognitoTokenIssuerEndpoint = "https://cognito-idp.ap-southeast-2.amazonaws.com/ap-southeast-2_9q6TXai99/.well-known/jwks.json"
                }); 
                
                var cache = new Mock<IMemoryCache>();
                cache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

                this.HttpContextAccessor = new Mock<IHttpContextAccessor>();

                var webClient = new Mock<IWebClient>();
                webClient.Setup(x => x.DownloadString(It.Is<string>(s => s.Contains("ap-southeast-2_D8bi2yzO7")))).Returns(otherIssuerKeys);
                webClient.Setup(x => x.DownloadString(It.Is<string>(s => s.Contains("ap-southeast-2_9q6TXai99")))).Returns(cognitoKeys);
                webClient.Setup(x => x.DownloadString(It.Is<string>(s => !s.Contains("cognito-idp")))).Returns(stsKeys);
                var webClientFactory = new Mock<IWebClientFactory>();
                webClientFactory.Setup(x => x.CreateWebClient()).Returns(webClient.Object);

                this.AccessTokenAuthorizationHandler = new AccessTokenAuthorizationHandler(this.HttpContextAccessor.Object, new TokenValidationService(cache.Object, webClientFactory.Object, settings, false));

                var requirements = new[] { new AccessTokenRequirement() };
                var user = new ClaimsPrincipal(new ClaimsIdentity());
                this.Context = new AuthorizationHandlerContext(requirements, user, null);
            }

            public AccessTokenAuthorizationHandler AccessTokenAuthorizationHandler { get; set; }
            public Mock<IHttpContextAccessor> HttpContextAccessor { get; set; }
            public AuthorizationHandlerContext Context { get; set; }

            public void HttpContextReturnsAccessToken(string accessToken)
            {
                var context = new DefaultHttpContext();
                context.Request.Headers[HeaderNames.Authorization] = accessToken;
                this.HttpContextAccessor.Setup(a => a.HttpContext).Returns(context);
            }
        }

        private const string validStsKeys = @"{""keys"":[{""kid"":""3E34069140783208D4D2C56A8A0BA1F524D0E252"",""use"":""sig"",""kty"":""RSA"",""alg"":""RS256"",""e"":""AQAB"",""n"":""trMxD8CmF8wRwsbNww0itX1cdDxdNwdUHzD3DAek2j5IrYcT7yUQekxgzbAB77pH0_WET4GOPFFoi45gj7Z5Bja2-xEBa7_eDxgi55MifydS3G8WS1vKnb3yHLzBnViDsDKhCNy7HNsWPjuRv3kXwonvd3Sf5LCDqY4zWm6HyqU5WWD2vVY-cIOhCr8yngLYolYSPGFdqUbZMvXPq8XOXOpy74mH3Uq3beazKddLLXIMr1Dwlb-10Dsgk68IP7oGJHMwYUZY_bgEM6Vl7dJDChJaSfXkDQRosZGDDNw68R52S_igZR68LA0vdQyNaqcHYeWyQsYsexCSxiK8F1A5VQ"",""x5t"":""PjQGkUB4MgjU0sVqiguh9STQ4lI"",""x5c"":[""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""]}]}";
        private const string invalidKidStsKeys = @"{""keys"":[{""kid"":""0000069140783208D4D2C56A8A0BA1F524D0E252"",""use"":""sig"",""kty"":""RSA"",""alg"":""RS256"",""e"":""AQAB"",""n"":""XXtrMxD8CmF8wRwsbNww0itX1cdDxdNwdUHzD3DAek2j5IrYcT7yUQekxgzbAB77pH0_WET4GOPFFoi45gj7Z5Bja2-xEBa7_eDxgi55MifydS3G8WS1vKnb3yHLzBnViDsDKhCNy7HNsWPjuRv3kXwonvd3Sf5LCDqY4zWm6HyqU5WWD2vVY-cIOhCr8yngLYolYSPGFdqUbZMvXPq8XOXOpy74mH3Uq3beazKddLLXIMr1Dwlb-10Dsgk68IP7oGJHMwYUZY_bgEM6Vl7dJDChJaSfXkDQRosZGDDNw68R52S_igZR68LA0vdQyNaqcHYeWyQsYsexCSxiK8F1A5VQ"",""x5t"":""PjQGkUB4MgjU0sVqiguh9STQ4lI"",""x5c"":[""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""]}]}";
        private const string validCognitoKeys = @"{""keys"":[{""alg"":""RS256"",""e"":""AQAB"",""kid"":""MXpvuBxFrJOfTtcXHFVud+OguUnhXNhL+wwaUP2IepQ="",""kty"":""RSA"",""n"":""kjFK4njRyGvNBaKeVXQuTya4fgMSItVy5D1tguPtEBuyAYKlRexapHOPNJ7T96zuQpW1fHQUWEWWNsvUBUbTGRbYA8gwxnsGd1WU4Np4QKT5N9jDHYsiYiOyTzm-d8tez_xHqgTiWLgCRCN-Uu5MYbFmfj3KlFON_2xlRZTQrFyD5AnAvjcsbM6o6kzRiATp0gRqGxnbx1ajqbzLSz5X_uFjwjlPh23RlzPImv4sYala3ZFUaDNKCxzDpXtRe7-rHXRW8W2OPqk4oIno4t7nAzRm1eNsvaIN5lzxW-Do_7UzXiBjqC1tZ4pzH_vmM-rSRWyzexDMNZ1xqGBUWgbh2w"",""use"":""sig""},{""alg"":""RS256"",""e"":""AQAB"",""kid"":""CwgO0uQYFUAsdSU4FOfIC1O1mTwbe0OTQvOIGvAz3nM="",""kty"":""RSA"",""n"":""k7ZJU5XtG_lIf5iQyJVHOJA1xM577IHYExYXxIi6YdM3hI5jqdsEyhG8AJ3aMLx5syww3YvQtsnhHXkzOs4WwRkMIZ1C99UWlorW0x4RZenYZ48lwUgmd2P6a8Fy1mudD0Hxo0KmKYbHVbCfJhjC1U_p9U7FoUyVj6WfVmXhQIIu_aN2cjCZ1c7_hY8OqZFjSbxzhfvowO-d5eqpVQ3rr40wVnGlzEZKixV1KwptJMVsiL-5uYNp5Tr0c5A9wHoptQv2lBnk-XAQiwfSQOt1AtARUk_z_gpjsYeZdxcUQtBmqXbwfp4hbJtgunJLNResKb2BkLLzyiKJ-gLS3ebZxw"",""use"":""sig""}]}";
        private const string invalidKidCognitoKeys = @"{""keys"":[{""alg"":""RS256"",""e"":""AQAB"",""kid"":""lHLXdSqhC65xLoyTieXbSRYVSKH2S40IoJAxPJ3FwBk="",""kty"":""RSA"",""n"":""t3cUaJ70vxXhPHVG5aOHyA99rxCzgQnKweHjrFcvWilIm5Q6W38GsqhzdEPD5zgLgZQM7QIR8GV5uGZcIcSyw1N6d5FZjnXCL6_BosUzESlUAq_Gpz54I4-ESxv-FQ1gc4hclT60nnaZ9g4h2rQIwLMRc7edic0roNDDqHhpWNOkzk0eWq7yWWTDMfCnUoVHODJ1qPsuOeCBZloh6ARYLNyVl-BUrm7fijrU2l2SisAyb5JAZveo98bGyLoUKknJy9SqiFcdrI__mJ8h5FXK1YFF-6MvJ_ko1Lagw56dOwT5iN3UtHBMl9o-3PGeInHHTdgXC2Wmue9MyB28w19Xgw"",""use"":""sig""},{""alg"":""RS256"",""e"":""AQAB"",""kid"":""000M7hkSRsUI27RUX2JCIFrZ4iOT8jcbPeHVFki1hvI="",""kty"":""RSA"",""n"":""x-qfSyT86kHer0xBqk4fwO18fwTtpRVCPc9QpXsdFKFqhnVSveSz4AMDZVo9P57NVlCs6SW3vih14Hd5JaTBQVky4qytmPehqFMWtzjGlB5eZkryJOqkXxK4eZg9wq7fYx7jBpbyo2Q8-VhzDmZOwE23DpDRuUA33DeiHI9Ybv9gJ2qxvsyckUoJFlLDHaGlbBtRrtsWLVTZJ52oCA0FepneIAnFS6ve0l1N2k2c9vDB4AGvgIlCx9Us4j0HLujtxo9padWkkJ0aCPkXQN_Nuua0U7bL9TLfT6Ivj-RU1JGDXwbY2PpbDLAUZZzaMM97qCr4DqpxTHPjMYdl5DHHnQ"",""use"":""sig""}]}";
        private const string otherIssuerKeys = @"{""keys"":[{""alg"":""RS256"",""e"":""AQAB"",""kid"":""lHLXdSqhC65xLoyTieXbSRYVSKH2S40IoJAxPJ3FwBk="",""kty"":""RSA"",""n"":""t3cUaJ70vxXhPHVG5aOHyA99rxCzgQnKweHjrFcvWilIm5Q6W38GsqhzdEPD5zgLgZQM7QIR8GV5uGZcIcSyw1N6d5FZjnXCL6_BosUzESlUAq_Gpz54I4-ESxv-FQ1gc4hclT60nnaZ9g4h2rQIwLMRc7edic0roNDDqHhpWNOkzk0eWq7yWWTDMfCnUoVHODJ1qPsuOeCBZloh6ARYLNyVl-BUrm7fijrU2l2SisAyb5JAZveo98bGyLoUKknJy9SqiFcdrI__mJ8h5FXK1YFF-6MvJ_ko1Lagw56dOwT5iN3UtHBMl9o-3PGeInHHTdgXC2Wmue9MyB28w19Xgw"",""use"":""sig""},{""alg"":""RS256"",""e"":""AQAB"",""kid"":""6I5M7hkSRsUI27RUX2JCIFrZ4iOT8jcbPeHVFki1hvI="",""kty"":""RSA"",""n"":""x-qfSyT86kHer0xBqk4fwO18fwTtpRVCPc9QpXsdFKFqhnVSveSz4AMDZVo9P57NVlCs6SW3vih14Hd5JaTBQVky4qytmPehqFMWtzjGlB5eZkryJOqkXxK4eZg9wq7fYx7jBpbyo2Q8-VhzDmZOwE23DpDRuUA33DeiHI9Ybv9gJ2qxvsyckUoJFlLDHaGlbBtRrtsWLVTZJ52oCA0FepneIAnFS6ve0l1N2k2c9vDB4AGvgIlCx9Us4j0HLujtxo9padWkkJ0aCPkXQN_Nuua0U7bL9TLfT6Ivj-RU1JGDXwbY2PpbDLAUZZzaMM97qCr4DqpxTHPjMYdl5DHHnQ"",""use"":""sig""}]}";

        private const string validStsToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjNFMzQwNjkxNDA3ODMyMDhENEQyQzU2QThBMEJBMUY1MjREMEUyNTIiLCJ0eXAiOiJKV1QifQ.eyJuYW1lIjoiTWljaGFlbCIsImdpdmVuX25hbWUiOiJNaWNoYWVsIiwiZW1haWwiOiJtaWNoYWVsLmJyeWRpZUBjYXNocmV3YXJkcy5jb20iLCJmYW1pbHlfbmFtZSI6IkJyeWRpZSIsInN1YiI6IjEwMDA4NTczODMiLCJtZW1iZXJfbmV3aWQiOiI1OTUxMjc1Yi02YjkwLTQ5MTMtYWZhOC00ZjliMDMyYWY2ZjYiLCJ0b2tlbl91c2FnZSI6ImFjY2Vzc190b2tlbiIsImp0aSI6ImI3MmIzN2I0LTMwNzUtNGZlOC05NWViLTE4MWVhY2YwNDM5NCIsImNmZF9sdmwiOiJwcml2YXRlIiwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsImVtYWlsIiwib2ZmbGluZV9hY2Nlc3MiXSwiYXVkIjpbImh0dHA6Ly9jYXNocmV3YXJkcy5jb20uYXUiLCJ0ZXN0IiwibTBkVG1Qa2tERHBQZnRLZ0Fpa3NlRDF2b09NPSIsIjQ2ZGFjN2U1MWYyNWVjYzhkZWVkOWZkNjdhZjU2NDI4IiwiOWEzOWZmMjdiM2IxYjI0OGFmMDhmYmRkMThjYTBlNGQiXSwiYXpwIjoiOWEzOWZmMjdiM2IxYjI0OGFmMDhmYmRkMThjYTBlNGQiLCJuYmYiOjE2MTYzODU1MzAsImV4cCI6MTYxNjQ3MTkzMCwiaWF0IjoxNjE2Mzg1NTMwLCJpc3MiOiJodHRwczovL3Rlc3Qtc3RzLmNhc2hyZXdhcmRzLmNvbS5hdS9zdGcifQ.bQ5A8BxoV4ufwENZFCbQ4AZoLuv4idAQRy25J690ySS3PRTQOLEPsB1JRyEQjO9onf4U7U714P0fe40Pa88BSw9VAzq7TYgM7ZJMXbcoq7ZwxCLyYMc8zM6aF3WKG1xjBUSpQ1wcDu3Rixv7Z3DDaQ5ydIpdmtxOK2hjBHQDtic62pQ1JE21mVwpHoqhf_m5dCuufNoUsNiO7nAmZTKiGTIgNqgwCp_rXSec2GTmhNtC2JC5asGZXEhwVz3y0qLkD39tr_-Jcf0ClpCl2GVF-y2iTSgXWW46ZPGCC4-e1N8xm5VdgzTLRfX3oUrVwovYzUTDalZQ-D5GJi10Zk0_-g";
        private const string invalidStsToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjNFMzQwNjkxNDA3ODMyMDhENEQyQzU2QThBMEJBMUY1MjREMEUyNTIiLCJ0eXAiOiJKV1QifQ.eyJuYW1lIjoiTWljaGFlbCIsImdpdmVuX25hbWUiOiJNaWNoYWVsIiwiZW1haWwiOiJzdGV2ZS5zbWl0aEBjYXNocmV3YXJkcy5jb20iLCJmYW1pbHlfbmFtZSI6IkJyeWRpZSIsInN1YiI6IjEwMDA4NTczODMiLCJtZW1iZXJfbmV3aWQiOiI1OTUxMjc1Yi02YjkwLTQ5MTMtYWZhOC00ZjliMDMyYWY2ZjYiLCJ0b2tlbl91c2FnZSI6ImFjY2Vzc190b2tlbiIsImp0aSI6ImI3MmIzN2I0LTMwNzUtNGZlOC05NWViLTE4MWVhY2YwNDM5NCIsImNmZF9sdmwiOiJwcml2YXRlIiwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsImVtYWlsIiwib2ZmbGluZV9hY2Nlc3MiXSwiYXVkIjpbImh0dHA6Ly9jYXNocmV3YXJkcy5jb20uYXUiLCJ0ZXN0IiwibTBkVG1Qa2tERHBQZnRLZ0Fpa3NlRDF2b09NPSIsIjQ2ZGFjN2U1MWYyNWVjYzhkZWVkOWZkNjdhZjU2NDI4IiwiOWEzOWZmMjdiM2IxYjI0OGFmMDhmYmRkMThjYTBlNGQiXSwiYXpwIjoiOWEzOWZmMjdiM2IxYjI0OGFmMDhmYmRkMThjYTBlNGQiLCJuYmYiOjE2MTYzODU1MzAsImV4cCI6MTYxNjQ3MTkzMCwiaWF0IjoxNjE2Mzg1NTMwLCJpc3MiOiJodHRwczovL3Rlc3Qtc3RzLmNhc2hyZXdhcmRzLmNvbS5hdS9zdGcifQ.bQ5A8BxoV4ufwENZFCbQ4AZoLuv4idAQRy25J690ySS3PRTQOLEPsB1JRyEQjO9onf4U7U714P0fe40Pa88BSw9VAzq7TYgM7ZJMXbcoq7ZwxCLyYMc8zM6aF3WKG1xjBUSpQ1wcDu3Rixv7Z3DDaQ5ydIpdmtxOK2hjBHQDtic62pQ1JE21mVwpHoqhf_m5dCuufNoUsNiO7nAmZTKiGTIgNqgwCp_rXSec2GTmhNtC2JC5asGZXEhwVz3y0qLkD39tr_-Jcf0ClpCl2GVF-y2iTSgXWW46ZPGCC4-e1N8xm5VdgzTLRfX3oUrVwovYzUTDalZQ-D5GJi10Zk0_-g";
        private const string validCognitoToken = "eyJraWQiOiJDd2dPMHVRWUZVQXNkU1U0Rk9mSUMxTzFtVHdiZTBPVFF2T0lHdkF6M25NPSIsImFsZyI6IlJTMjU2In0.eyJzdWIiOiJhMDNkNzRmMi0yNmY4LTQ5ODItOTQyNi02ZjU1NzVlMmVlZDYiLCJldmVudF9pZCI6ImU2NzllMmE2LWUzYWItNGMwYi05NDE3LTk5NjZlNDI4YWU4ZiIsInRva2VuX3VzZSI6ImFjY2VzcyIsInNjb3BlIjoiYXdzLmNvZ25pdG8uc2lnbmluLnVzZXIuYWRtaW4gcGhvbmUgb3BlbmlkIHByb2ZpbGUgZW1haWwiLCJhdXRoX3RpbWUiOjE2MTY5OTg3NDksImlzcyI6Imh0dHBzOlwvXC9jb2duaXRvLWlkcC5hcC1zb3V0aGVhc3QtMi5hbWF6b25hd3MuY29tXC9hcC1zb3V0aGVhc3QtMl85cTZUWGFpOTkiLCJleHAiOjE2MTcwMDIzNDksImlhdCI6MTYxNjk5ODc1MCwidmVyc2lvbiI6MiwianRpIjoiNDRiMTEwZWMtZTc2Yy00Nzg4LWFhM2MtMWY5YTc4ZDE2MjdmIiwiY2xpZW50X2lkIjoiNjU5Z2FlMmxyNGNnajRzbGxmb3ZrODVpdHQiLCJ1c2VybmFtZSI6IjU5NTEyNzViLTZiOTAtNDkxMy1hZmE4LTRmOWIwMzJhZjZmNiJ9.ADQ9SksqdrstbvZeEnEJ3HY690UzSz-DzPGCAPKZJm6MrY6pZFd-uVkul0hy_0GC-JAZ03V5eTsAKddPBWzZusdza-BXNvzW20Un3h_J5qEymtQGaX2KlrW9BM8vzekEyyL3irpFIHWlC-Q7i-3heEBWN0d7yTO5OujGNOhPMCR7sa1E3sPGUv3kb2HwRw6wDW0O_kg9YbDNMREM_6sYPuDX8vKO4t2BZhsiPXKmIyDq3hp6Vh0UBCvZOqeYMwu9vLm52tyEsZFssEdYesjxFXAKiZBEibaDYUc0Ad0m9TwhMnjx3WG_xp1DpzZs53YBJ_LgHxTadPrb4CVBOcsvwg";
        private const string invalidCognitoToken = "eyJraWQiOiJDd2dPMHVRWUZVQXNkU1U0Rk9mSUMxTzFtVHdiZTBPVFF2T0lHdkF6M25NPSIsImFsZyI6IlJTMjU2In0.eyJzdWIiOiJhMDNkNzRmMi0yNmY4LTQ5ODItOTQyNi02ZjU1NzVlMmVlZDYiLCJldmVudF9pZCI6ImU2NzllMmE2LWUzYWItNGMwYi05NDE3LTk5NjZlNDI4YWU4ZiIsInRva2VuX3VzZSI6ImFjY2VzcyIsInNjb3BlIjoiYXdzLmNvZ25pdG8uc2lnbmluLnVzZXIuYWRtaW4gcGhvbmUgb3BlbmlkIHByb2ZpbGUgZW1haWwiLCJhdXRoX3RpbWUiOjE2MTY5OTg3NDksImlzcyI6Imh0dHBzOlwvXC9jb2duaXRvLWlkcC5hcC1zb3V0aGVhc3QtMi5hbWF6b25hd3MuY29tXC9hcC1zb3V0aGVhc3QtMl85cTZUWGFpOTkiLCJleHAiOjE2MTcwMDIzNDksImlhdCI6MTYxNjk5ODc1MCwidmVyc2lvbiI6MiwianRpIjoiNDRiMTEwZWMtZTc2Yy00Nzg4LWFhM2MtMWY5YTc4ZDE2MjdmIiwiY2xpZW50X2lkIjoiNjU5Z2FlMmxyNGNnajRzbGxmb3ZrODVpdHQiLCJ1c2VybmFtZSI6IlN0ZXZlLlNtaXRoIn0.ADQ9SksqdrstbvZeEnEJ3HY690UzSz-DzPGCAPKZJm6MrY6pZFd-uVkul0hy_0GC-JAZ03V5eTsAKddPBWzZusdza-BXNvzW20Un3h_J5qEymtQGaX2KlrW9BM8vzekEyyL3irpFIHWlC-Q7i-3heEBWN0d7yTO5OujGNOhPMCR7sa1E3sPGUv3kb2HwRw6wDW0O_kg9YbDNMREM_6sYPuDX8vKO4t2BZhsiPXKmIyDq3hp6Vh0UBCvZOqeYMwu9vLm52tyEsZFssEdYesjxFXAKiZBEibaDYUc0Ad0m9TwhMnjx3WG_xp1DpzZs53YBJ_LgHxTadPrb4CVBOcsvwg";
        private const string otherIssuerToken = "eyJraWQiOiI2STVNN2hrU1JzVUkyN1JVWDJKQ0lGclo0aU9UOGpjYlBlSFZGa2kxaHZJPSIsImFsZyI6IlJTMjU2In0.eyJzdWIiOiJhNmE5YTU0Yy1iNTQzLTQ4YTUtODQ0ZC00YTYwZWY3ZTYzYzQiLCJldmVudF9pZCI6ImE2ZDU2Y2Q4LTJkYTEtNDYyMS1iNTNiLTkyODIxY2RlNDEwMCIsInRva2VuX3VzZSI6ImFjY2VzcyIsInNjb3BlIjoib3BlbmlkIHByb2ZpbGUgZW1haWwiLCJhdXRoX3RpbWUiOjE2MTc4NTM3MTksImlzcyI6Imh0dHBzOlwvXC9jb2duaXRvLWlkcC5hcC1zb3V0aGVhc3QtMi5hbWF6b25hd3MuY29tXC9hcC1zb3V0aGVhc3QtMl9EOGJpMnl6TzciLCJleHAiOjE2MTc4NTczMTksImlhdCI6MTYxNzg1MzcxOSwidmVyc2lvbiI6MiwianRpIjoiZjk0N2EyYjEtMDQ2Ny00MmZkLWIyYzAtMTIyOTI5ZjViNjkxIiwiY2xpZW50X2lkIjoiNnA1amxmZzd1MGZsMnU4NXJ0YmgzcHJtYmkiLCJ1c2VybmFtZSI6Im1pY2hhZWwuYnJ5ZGllQGNhc2hyZXdhcmRzLmNvbSJ9.Nh8D84xBsFiq11hlzeS14SQJ18N74rPmT73EmB-JFPvQNT7gMak1tTOWYNOoIg-fJ_uZJgP25RPTlHM7Wk6jcNFlI_VFDAB7UmxJXcCFBviK6X4Mx3qL7K4xMJOETvVr5DxYdkTEME-T45HEsBPmxs0VRY_jhHSA81726B9WEnxs3WdiKyk16sXqcG-NUmjz6eBDS0tK9lYUKsW7Je_cp-_fMl-Xo_PkLe2P5GJn3Z6XYz0pXtHN50lZ0IZKdXb2AQozztwz-O8h5OGkybNPqoPPZVvEZLLoQ-EDfIUfQIUj-8bhJNj673KPSj1UGbZ-mh2LfLqXfEadESFU8S8Xrw";

        [Fact]
        public async Task AuthHandler_ShouldSetMemberIdClaim_GivenValidStsToken()
        {
            var state = new TestState();

            state.HttpContextReturnsAccessToken(validStsToken);

            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.User.Claims.First(c => c.Type == "memberId").Value.Should().Be("1000857383");
        }

        [Fact]
        public async Task AuthHandler_ShouldSetCognitoIdClaim_GivenValidCognitoToken()
        {
            var state = new TestState();

            state.HttpContextReturnsAccessToken(validCognitoToken);

            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.User.Claims.First(c => c.Type == "cognitoId").Value.Should().Be("5951275b-6b90-4913-afa8-4f9b032af6f6");
        }

        [Fact]
        public async Task AuthHandler_ShouldSetMemberIdClaim_GivenNullToken()
        {
            var state = new TestState();

            state.HttpContextReturnsAccessToken(null);

            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AuthHandler_ShouldThrowException_GivenValidStsToken_AndGivenNoMatchingKeyIsFound()
        {
            var state = new TestState(invalidKidStsKeys);

            state.HttpContextReturnsAccessToken(validStsToken);
            
            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AuthHandler_ShouldThrowException_GivenInvalidStsToken()
        {
            var state = new TestState();

            state.HttpContextReturnsAccessToken(invalidStsToken);

            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AuthHandler_ShouldThrowException_GivenValidCognitoToken_AndGivenNoMatchingKeyIsFound()
        {
            var state = new TestState(validStsKeys, invalidKidCognitoKeys);

            state.HttpContextReturnsAccessToken(validCognitoToken);

            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AuthHandler_ShouldThrowException_GivenInvalidCognitoToken()
        {
            var state = new TestState();

            state.HttpContextReturnsAccessToken(invalidCognitoToken);

            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AuthHandler_ShouldThrowException_GivenValidTokenFromSomeOtherIssuer()
        {
            var state = new TestState();

            state.HttpContextReturnsAccessToken(otherIssuerToken);

            await state.AccessTokenAuthorizationHandler.HandleAsync(state.Context);

            state.Context.HasSucceeded.Should().BeFalse();
        }

    }
}
