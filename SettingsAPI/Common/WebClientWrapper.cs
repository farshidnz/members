using System;
using System.Net;

namespace SettingsAPI.Common
{
    public interface IWebClient : IDisposable
    {
        string DownloadString(string address);
    }

    public class WebClientWrapper : IWebClient, IDisposable
    {
        private readonly WebClient _webClient;

        private bool isDisposed;

        public WebClientWrapper()
        {
            _webClient = new WebClient();
        }

        public string DownloadString(string address) => _webClient.DownloadString(address);

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    _webClient.Dispose();
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
