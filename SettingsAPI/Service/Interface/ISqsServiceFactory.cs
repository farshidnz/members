using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Service.Interface
{
    public interface ISqsServiceFactory
    {
        Task<ISqsService> CreateSqsService(string sqsQueueName);
    }
}