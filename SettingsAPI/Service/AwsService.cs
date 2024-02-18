using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SettingsAPI.Common;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace SettingsAPI.Service
{
    public class AwsService : IAwsService
    {
        private readonly IOptions<Settings> _settings;
        private AmazonSimpleNotificationServiceClient _clientSns;
        private AmazonSQSClient _clientSqs;
        private string _queueUrl;

        public AwsService(IOptions<Settings> settings)
        {
            _settings = settings;
        }

        public async Task<IEnumerable<string>> FetchBsbData()
        {
            var lines = await Util.ReadAmazonS3Data(_settings.Value.BsbKey, _settings.Value.BsbBucketName);
            var lineArr = lines.Split('\n');

            // Only take the first column which is bsb number and get rid of the "-"
            var csv = from line in lineArr
                select line.Split(',')[0].Replace("-", string.Empty).Replace("\"", string.Empty);

            return csv;
        }

        public async Task SendSnsMessage(object objMessage)
        {
            var message = JsonConvert.SerializeObject(objMessage);
            var request = new PublishRequest
            {
                Message = message,
                TopicArn = _settings.Value.TopicArnMemberUpdatedEvent,
                MessageStructure = "Raw",
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "MessageType",
                        new MessageAttributeValue {DataType = "String", StringValue = message.GetType().FullName}
                    },
                    {
                        "CorrelationId",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = Trace.CorrelationManager.ActivityId.ToString()
                        }
                    }
                }
            };
            await ClientSns.PublishAsync(request).ConfigureAwait(false);
        }

        public async Task SendSqsMessage(object objMessage, string messageGroupId)
        {
            var messageType = objMessage.GetType().FullName;
            var messageBody = JsonConvert.SerializeObject(objMessage);
            
            var request = new SendMessageRequest
            {
                MessageBody = messageBody,
                QueueUrl = QueueUrl,
                MessageAttributes = new Dictionary<string, Amazon.SQS.Model.MessageAttributeValue>
                {
                    {
                        "MessageType",
                        new Amazon.SQS.Model.MessageAttributeValue {DataType = "String", StringValue = messageType}
                    },
                    {
                        "CorrelationId",
                        new Amazon.SQS.Model.MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = Trace.CorrelationManager.ActivityId.ToString()
                        }
                    }
                }
            };
            if (!string.IsNullOrWhiteSpace(messageGroupId))
                request.MessageGroupId = messageGroupId;

            await ClientSqs.SendMessageAsync(request).ConfigureAwait(false);
        }

        private AmazonSimpleNotificationServiceClient ClientSns
        {
            get
            {
                _clientSns = _clientSns switch
                {
                    null => new AmazonSimpleNotificationServiceClient(),
                    _ => _clientSns
                };
                return _clientSns;
            }
        }

        private AmazonSQSClient ClientSqs
        {
            get
            {
                _clientSqs = _clientSqs switch
                {
                    null => new AmazonSQSClient(),
                    _ => _clientSqs
                };

                return _clientSqs;
            }
        }

        private string QueueUrl
        {
            get
            {
                _queueUrl = _queueUrl switch
                {
                    null => ClientSqs.GetQueueUrlAsync(_settings.Value.CognitoQueueName).Result.QueueUrl,
                    _ => _queueUrl
                };
                return _queueUrl;
            }
        }
    }
}