using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class LeanplumService : ILeanplumService
    {
        private class SetUserAttributesRequest
        {
            public string appId { get; set; }
            public string clientKey { get; set; }
            public string apiVersion { get; set; }
            public string userId { get; set; }
            public Dictionary<string, object> userAttributes;
        }


        private const string ApiEndpoint = "https://api.leanplum.com/api";
        private const string ApiVersion = "1.0.6";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<Settings> _options;

        public LeanplumService(IHttpClientFactory httpClientFactory, IOptions<Settings> options)
        {
            this._httpClientFactory = httpClientFactory;
            this._options = options;
        }

        public async Task SetMemberAttribute(Guid leanplumMemberId, string key, bool value)
        {
            await SendSetMemberAttribute(leanplumMemberId, key, value);
        }

        public async Task SetMemberAttribute(Guid leanplumMemberId, string key, string value)
        {
            await SendSetMemberAttribute(leanplumMemberId, key, value);
        }

        private async Task SendSetMemberAttribute(Guid leanplumMemberId, string key, object value)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new SetUserAttributesRequest()
            {
                appId = _options.Value.LeanplumAppId,
                clientKey = _options.Value.LeanplumClientKey,
                apiVersion = ApiVersion,
                userId = leanplumMemberId.ToString().ToUpper(),
                userAttributes = new Dictionary<string, object>()
                {
                    { key, value }
                }
            };

            var serializedRequest = JsonConvert.SerializeObject(request);

            var content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(ApiEndpoint + "?action=setUserAttributes", content);

            if(!response.IsSuccessStatusCode)
            {
                throw new Exception($"Leanplum SetMemberAttribute failed with: {response.StatusCode}");
            }
        }

    }
}
