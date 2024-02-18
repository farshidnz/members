using System.Collections.Generic;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public interface IAwsService
    {
        Task<IEnumerable<string>> FetchBsbData();
        Task SendSnsMessage(object objMessage);

        Task SendSqsMessage(object objMessage, string messageGroupId);
    }
}