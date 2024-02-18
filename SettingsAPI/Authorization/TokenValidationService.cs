using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SettingsAPI.Common;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;

namespace SettingsAPI.Authorization
{
    public class TokenValidationService : ITokenValidation
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IWebClientFactory _webClientFactory;
        private IOptions<Settings> _settings;
        private bool _validateLifetime;

        public TokenValidationService(IMemoryCache memoryCache, IWebClientFactory webClientFactory, IOptions<Settings> settings, bool validateLifetime = true)
        {
            this._memoryCache = memoryCache;
            this._webClientFactory = webClientFactory;
            this._settings = settings;
            this._validateLifetime = validateLifetime;
        }

        public JwtSecurityToken ValidateToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = null;

            var allowedIssuers = new string[]
            {
                _settings.Value.StsTokenIssuerEndpoint,
                _settings.Value.CognitoTokenIssuerEndpoint
            };
            var tokenIssuer = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Issuer;
            var issuerEndpoint = allowedIssuers.FirstOrDefault(allowedIssuer => GetIssuer(allowedIssuer) == tokenIssuer);
            
            if (string.IsNullOrEmpty(issuerEndpoint)) 
                throw new ArgumentNullException(nameof(issuerEndpoint));

            handler.ValidateToken(accessToken, GetTokenValidationParameters(issuerEndpoint), out SecurityToken validatedToken);
            jwtToken = (JwtSecurityToken)validatedToken;

            return jwtToken;
        }

        private static string GetIssuer(string issuerEndpoint) => issuerEndpoint.Substring(0, issuerEndpoint.IndexOf("/.well-known/jwks"));


        private TokenValidationParameters GetTokenValidationParameters(string issuerEndpoint)
        {
            return new TokenValidationParameters
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidIssuer = GetIssuer(issuerEndpoint),
                ValidateIssuerSigningKey = true,
                ValidateAudience = false,
                IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
                {
                    string issuerToken = (string)_memoryCache.Get(issuerEndpoint);
                    if (string.IsNullOrEmpty(issuerToken))
                        if (!_memoryCache.TryGetValue(issuerEndpoint, out issuerToken))
                        {
                            using (var webClient = _webClientFactory.CreateWebClient())
                            {
                                issuerToken = webClient.DownloadString(issuerEndpoint);
                            }

                            var cacheEntryOptions = new MemoryCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromSeconds(600));

                            _memoryCache.Set(issuerEndpoint, issuerToken, cacheEntryOptions);
                        }
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<JsonWebKeySet>(issuerToken).Keys;
                },
                ValidateLifetime = _validateLifetime,
                // Allow for some drift in server time
                // (a lower value is better; we recommend two minutes or less)
                ClockSkew = TimeSpan.FromMinutes(2),
            };
        }
    }
}
