using System;

namespace SettingsAPI.Common
{
    public interface IWebClientFactory
    {
        IWebClient CreateWebClient();
    }

    public class WebClientFactory : IWebClientFactory
    {
        public IWebClient CreateWebClient() => new WebClientWrapper();
    }
}
