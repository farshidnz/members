using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public interface IPaypalApiService
    {
        Task<T> ExecuteAsyncCallApi<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue authHeader,
            string content = null);
        
    }
}