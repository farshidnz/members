using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SettingsAPI.Service.Interface
{
    public interface ISqsService
    {
        IAsyncEnumerable<Message> ReadMessageStream(CancellationToken stoppingToken);

        Task<bool> DeleteMessage(Message message);

        string QueueName { get; }

        string QueueURL { get; }
    }
}