using Amazon.SQS.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SettingsAPI.BackgroundHostedService;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SettingsAPI.Tests.BackgroundHostedService
{
    public class MemberCreatedEventHandlerServiceTests
    {
        private Mock<IMemoryCache> memoryCache;
        private Mock<IOptions<Settings>> settings;
        private Mock<ILogger<MemberCreatedEventHandlerService>> logger;
        private Mock<IMemberService> memberService;
        private Mock<ISqsService> sqsService;
        private Mock<ISqsServiceFactory> sqsServiceFactory;
        private Mock<IServiceProvider> serviceProvider;
        private Mock<IServiceScope> serviceScope;

        [SetUp]
        public void Setup()
        {
            memoryCache = new Mock<IMemoryCache>();
            memoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

            settings = new Mock<IOptions<Settings>>();
            settings.Setup(x => x.Value).Returns(new Settings() { 
                MemberCreatedQueueName = "MemberSettingsAPI-dev-MemberCreatedEventQueue",
                SendVerificationEmail = true
            });

            logger = new Mock<ILogger<MemberCreatedEventHandlerService>>();
            memberService = new Mock<IMemberService>();

            sqsService = new Mock<ISqsService>();

            sqsServiceFactory = new Mock<ISqsServiceFactory>();
            sqsServiceFactory.Setup(x => x.CreateSqsService(It.IsAny<string>())).Returns(Task.FromResult(sqsService.Object));

            serviceProvider = new Mock<IServiceProvider>();
            serviceScope = new Mock<IServiceScope>();
        }

        public MemberCreatedEventHandlerService SUT()
        {
            // partial mock
            var sut = new Mock<MemberCreatedEventHandlerService>(logger.Object, settings.Object, memoryCache.Object, sqsServiceFactory.Object, serviceProvider.Object)
            {
                CallBase = true
            };
            sut.Setup(x => x.CreateServiceScope()).Returns(serviceScope.Object);
            sut.Setup(x => x.GetMemberService(It.IsAny<IServiceScope>())).Returns(memberService.Object);
            return sut.Object;
        }

        [Test]
        public async Task WhenMemberCreatedQueueNameNotFoundInSettings_ShouldDoNothing()
        {
            settings.Setup(x => x.Value).Returns(new Settings() { MemberCreatedQueueName = null });

            var service = SUT();

            await service.ProcessMemberCreatedEvents(default);

            sqsService.Verify(x => x.ReadMessageStream(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task WhenErrorInCreatingSqsService_ShouldLogError_AndDoNothing()
        {
            Exception e = new Exception("error creating sqs service");
            sqsServiceFactory.Setup(x => x.CreateSqsService(It.IsAny<string>())).Throws(e);

            var service = SUT();

            await service.ProcessMemberCreatedEvents(default);

            sqsService.Verify(x => x.ReadMessageStream(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task WhenMessageReadFromSQSqueue_ShouldProcessMessage_ToSendVerificationEmail()
        {
            sqsService.Setup(x => x.ReadMessageStream(It.IsAny<CancellationToken>())).Returns(GetTestMessage);

            await SUT().ProcessMemberCreatedEvents(default);

            memberService.Verify(x => x.SendSignupAutomatedVerificationEmail(101), Moq.Times.Once);
        }

        [Test]
        public async Task WhenMessageReadFromSQSqueue_AndSendVerificationEmailIsFalse_ShouldNotSendEmail()
        {
            sqsService.Setup(x => x.ReadMessageStream(It.IsAny<CancellationToken>())).Returns(GetTestMessage);
            settings.Setup(x => x.Value).Returns(new Settings() { 
                MemberCreatedQueueName = "MemberSettingsAPI-dev-MemberCreatedEventQueue",
                SendVerificationEmail = false
            });

            await SUT().ProcessMemberCreatedEvents(default);

            memberService.Verify(x => x.SendSignupAutomatedVerificationEmail(It.IsAny<int>()), Moq.Times.Never);
        }

        [Test]
        public async Task WhenErrorInSendingEmail_ShouldRetrySendingVerificationEmail()
        {
            sqsService.Setup(x => x.ReadMessageStream(It.IsAny<CancellationToken>())).Returns(GetTestMessage);
            memberService.Setup(x => x.SendSignupAutomatedVerificationEmail(101)).Throws(new Exception());

            var sut = SUT();
            await sut.ProcessMemberCreatedEvents(default);

            memberService.Verify(x => x.SendSignupAutomatedVerificationEmail(101), Moq.Times.Exactly(sut.MAX_NUM_EMAIL_SEND_RETRIES));
        }

        private async IAsyncEnumerable<Message> GetTestMessage()
        {
            yield return new Message()
            {
                Body = "{ memberid: 101 }"
            };

            await Task.CompletedTask;
        }

        private async IAsyncEnumerable<Message> GetInvalidMessage()
        {
            yield return new Message()
            {
                Body = "garbage"
            };

            await Task.CompletedTask;
        }
    }
}