using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class SqsService : ISqsService
    {
        private readonly ILogger<SqsService> logger;
        private readonly IAmazonSQS sqsClient;
        private readonly IMemoryCache memoryCache;
        private string queueName;
        private string queueUrl;

        // take 3 min break if nothing to read
        public virtual Task TakeBreakBeforePollingForMessages(CancellationToken stoppingToken) => Task.Delay(3 * 60 * 1000, stoppingToken);

        private string MessageKey(Message message) => $"{QueueName}-{message.MD5OfBody}";

        private void CacheReadMessages(Message message) => memoryCache.Set(MessageKey(message), message.Body, TimeSpan.FromMinutes(60));

        public virtual bool IsDuplicate(Message message) => memoryCache.TryGetValue(MessageKey(message), out string _);

        public virtual bool CancellationRequested(CancellationToken stoppingToken) => stoppingToken.IsCancellationRequested;

        public SqsService(IAmazonSQS sqsClient, IMemoryCache memoryCache, ILogger<SqsService> logger)
        {
            this.logger = logger;
            this.sqsClient = sqsClient;
            this.memoryCache = memoryCache;
        }

        public async Task Initialise(string sqsQueueName)
        {
            if (string.IsNullOrWhiteSpace(sqsQueueName))
            {
                throw new ArgumentException("Invalid queue name", sqsQueueName);
            }

            queueName = sqsQueueName;
            var queueUrlResponse = await sqsClient.GetQueueUrlAsync(sqsQueueName);
            queueUrl = queueUrlResponse.QueueUrl;
        }

        public string QueueName
        {
            get { return queueName; }
        }

        public string QueueURL
        {
            get { return queueUrl; }
        }

        // may read duplicate messages(as per normal SQS design), therefore consumers expected to be idempotent
        public virtual async IAsyncEnumerable<Message> ReadMessageStream([EnumeratorCancellation] CancellationToken stoppingToken)
        {
            while (!CancellationRequested(stoppingToken))
            {
                var (hasMessages, messages) = await ReadMessages();
                if (!hasMessages)
                {
                    await TakeBreakBeforePollingForMessages(stoppingToken);
                    continue;
                }

                // request read messages to be removed from queue so they wont get processed again,
                // but this is not guaranteed! So message consumers expected to be idempotent
                var requestClearReadMsgFromQueue = BulkDeleteMessages(messages);

                foreach (var message in messages)
                {
                    if (IsDuplicate(message))
                    {
                        continue;
                    }

                    CacheReadMessages(message);
                    yield return message;
                }

                await requestClearReadMsgFromQueue;
            }
        }

        public virtual async Task<(bool hasMessages, IEnumerable<Message> messages)> ReadMessages()
        {
            try
            {
                var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = QueueURL,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20
                });

                return (response.Messages.Count > 0, response.Messages);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"SQS read error: {e.Message}");
                return (false, default);
            }
        }

        public virtual async Task<bool> DeleteMessage(Message message)
        {
            try
            {
                var response = await sqsClient.DeleteMessageAsync(QueueURL, message.ReceiptHandle);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"SQS delete error: {e.Message}");
                return false;
            }
        }

        public virtual async Task<bool> BulkDeleteMessages(IEnumerable<Message> messages)
        {
            try
            {
                var response = await sqsClient.DeleteMessageBatchAsync(QueueURL,
                                            messages.Select(x => new DeleteMessageBatchRequestEntry(x.MessageId, x.ReceiptHandle)).ToList());
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"SQS bulk delete error: {e.Message}");
                return false;
            }
        }
    }
}
