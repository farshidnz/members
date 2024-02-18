using Amazon.SQS.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SettingsAPI.Model.Dto;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SettingsAPI.BackgroundHostedService
{
    public class MemberCreatedEventHandlerService : BackgroundService
    {
        private readonly ILogger<MemberCreatedEventHandlerService> logger;
        private readonly IOptions<Settings> settings;
        private readonly IMemoryCache memoryCache;
        private readonly ISqsServiceFactory sqsServiceFactory;
        private readonly IServiceProvider serviceProvider;
        public readonly string ServiceName = "MemberCreatedEventHandler";
        public readonly int MAX_NUM_EMAIL_SEND_RETRIES = 3;

        public MemberCreatedEventHandlerService(ILogger<MemberCreatedEventHandlerService> logger,
                                                IOptions<Settings> settings,
                                                IMemoryCache memoryCache,
                                                ISqsServiceFactory sqsServiceFactory,
                                                IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.settings = settings;
            this.memoryCache = memoryCache;
            this.sqsServiceFactory = sqsServiceFactory;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => logger.LogInformation($"{ServiceName} background task is stopping due to CancellationToken."));

            await ProcessMemberCreatedEvents(stoppingToken);

            logger.LogInformation($"{ServiceName} background task is stopping.");
        }

        public async Task ProcessMemberCreatedEvents(CancellationToken stoppingToken)
        {
            var memberCreatedQueueName = settings.Value?.MemberCreatedQueueName;
            if (string.IsNullOrWhiteSpace(memberCreatedQueueName))
            {
                logger.LogError($"{ServiceName} could not read valid MemberCreatedQueueName from settings.");
                return;
            }

            logger.LogInformation($"{ServiceName} is attempting to read SQS : {memberCreatedQueueName}.");
            var sqsService = await GetSqsService(memberCreatedQueueName);
            if (sqsService != null)
            {
                logger.LogInformation($"{ServiceName} is starting to read and process messages from SQS : {sqsService.QueueURL}.");

                await foreach (var message in sqsService.ReadMessageStream(stoppingToken))
                {
                    await ProcessMessage(message);
                }
            }
        }

        private async Task<bool> ProcessMessage(Message message)
        {
            var memberCreatedEvent = GetMemberCreatedEvent(message);
            if (memberCreatedEvent == null)
            {
                return true;
            }

            if (IsEmailAlreadySentForMember(memberCreatedEvent))
            {
                // defensive code in case duplicate messages get read from sqs
                return true;
            }

            if(!this.settings.Value.SendVerificationEmail)
            {
                return true;
            }

            logger.LogInformation($"{ServiceName} sending verification email to member : {memberCreatedEvent.MemberId}, {memberCreatedEvent.Email} " +
                                  $"(messageId : {message.MessageId}, md5body : {message.MD5OfBody}).");

            return await SendVerificationEmailForMember(memberCreatedEvent);
        }

        private async Task<bool> SendVerificationEmailForMember(MemberCreatedEvent memberCreatedEvent)
        {
            var retryCount = 0;
            do
            {
                if (retryCount > 0)
                {
                    logger.LogInformation($"{ServiceName} retrying sending verification email to member : {memberCreatedEvent.MemberId}, {memberCreatedEvent.Email}");
                }

                if (await SendVerificationEmail(memberCreatedEvent))
                {
                    CacheSentEmailEvent(memberCreatedEvent);
                    return true;
                }
            } while (++retryCount < MAX_NUM_EMAIL_SEND_RETRIES);

            return false;
        }

        private async Task<bool> SendVerificationEmail(MemberCreatedEvent memberCreatedEvent)
        {
            try
            {
                using var scope = CreateServiceScope();
                await GetMemberService(scope).SendSignupAutomatedVerificationEmail(memberCreatedEvent.MemberId);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{ServiceName} failed to send verification email for member : {memberCreatedEvent.MemberId}," +
                                   $" Error: {e.Message}");
                return false;
            }
        }

        public virtual IServiceScope CreateServiceScope() => serviceProvider.CreateScope();

        public virtual IMemberService GetMemberService(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMemberService>();

        private string EventKey(MemberCreatedEvent memberCreatedEvent) => $"{ServiceName}-{memberCreatedEvent.MemberId}";

        private void CacheSentEmailEvent(MemberCreatedEvent memberCreatedEvent) => memoryCache.Set(EventKey(memberCreatedEvent), memberCreatedEvent, TimeSpan.FromMinutes(60));

        private bool IsEmailAlreadySentForMember(MemberCreatedEvent memberCreatedEvent) => memoryCache.TryGetValue(EventKey(memberCreatedEvent), out MemberCreatedEvent _);

        private MemberCreatedEvent GetMemberCreatedEvent(Message message)
        {
            try
            {
                return JsonConvert.DeserializeObject<MemberCreatedEvent>(message.Body);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{ServiceName} failed to deserialise message : {message.Body}.");
                return null;
            }
        }

        private async Task<ISqsService> GetSqsService(string memberCreatedEventQueue)
        {
            try
            {
                return await sqsServiceFactory.CreateSqsService(memberCreatedEventQueue);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{ServiceName} failed to create SQS service : {e.Message}.");
                return null;
            }
        }
    }
}