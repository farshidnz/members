using Amazon.SQS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SettingsAPI.Service.Interface;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class SqsServiceFactory : ISqsServiceFactory
    {
        private readonly ConcurrentDictionary<string, SqsService> services = new ConcurrentDictionary<string, SqsService>();
        private readonly IAmazonSQS sqsClient;
        private readonly IMemoryCache memoryCache;
        private readonly ILoggerFactory loggerFactory;

        public SqsServiceFactory(IAmazonSQS sqsClient, IMemoryCache memoryCache, ILoggerFactory loggerFactory)
        {
            this.sqsClient = sqsClient;
            this.memoryCache = memoryCache;
            this.loggerFactory = loggerFactory;
        }

        public virtual async Task<ISqsService> CreateSqsService(string sqsQueueName)
        {
            if (!services.TryGetValue(sqsQueueName, out SqsService sqsService))
            {
                sqsService = new SqsService(sqsClient, memoryCache, loggerFactory.CreateLogger<SqsService>());
                await sqsService.Initialise(sqsQueueName);
                services[sqsQueueName] = sqsService;
            }

            return sqsService;
        }
    }
}