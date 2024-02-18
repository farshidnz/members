using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SettingsAPI.Error;

namespace SettingsAPI.Service
{
    public class PaypalApiService : IPaypalApiService
    {
        public async Task<T> ExecuteAsyncCallApi<T>(HttpMethod method, string endpoint,
            AuthenticationHeaderValue authHeader, string content = null)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var request = new HttpRequestMessage(method, new Uri(endpoint))
            {
                Content = content != null
                    ? new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
                    : null,
            };

            request.Headers.Authorization = authHeader;
            HttpResponseMessage response;
            try
            {
                response = (await client.SendAsync(request).ConfigureAwait(false)).EnsureSuccessStatusCode();
            }
            catch(Exception ex)
            {
                throw new PaypalAuthorizationCodeUnauthorizedException(ex.Message);
            }

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }
    }
}