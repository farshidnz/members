using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SettingsAPI.BackgroundHostedService;
using SettingsAPI.Model.Dto;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SettingsAPI.Tests.Service
{
    public class SqsServiceTests
    {
        private Mock<IAmazonSQS> sqsClient;
        private Mock<IMemoryCache> memoryCache;
        private Mock<ILogger<SqsService>> logger;
        private Mock<ILoggerFactory> loggerFactory;

        [SetUp]
        public void Setup()
        {
            sqsClient = new Mock<IAmazonSQS>();
            memoryCache = new Mock<IMemoryCache>();
            memoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

            logger = new Mock<ILogger<SqsService>>();
            loggerFactory = new Mock<ILoggerFactory>();
        }

        public async Task<ISqsService> FactoryMethod(string queueName = "testQ")
        {
            var factory = new SqsServiceFactory(sqsClient.Object, memoryCache.Object, loggerFactory.Object);
            return await factory.CreateSqsService(queueName);
        }

        public Mock<SqsService> SUT()
        {
            // partial mock
            var sut = new Mock<SqsService>(sqsClient.Object, memoryCache.Object, logger.Object);
            sut.CallBase = true;
            sut.SetupSequence(x => x.CancellationRequested(It.IsAny<CancellationToken>()))
                               .Returns(false)
                               .Returns(true);
            sut.Setup(x => x.TakeBreakBeforePollingForMessages(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return sut;
        }

        [Test]
        public async Task Factory_ShouldInitialiseSqsService_BasedOnQueueName()
        {
            var queueName = "testQ";

            sqsClient.Setup(x => x.GetQueueUrlAsync(queueName, default)).Returns(Task.FromResult(new GetQueueUrlResponse
            {
                QueueUrl = "testQ URL"
            }));

            var service = await FactoryMethod(queueName);
            service.QueueName.Should().Be(queueName);
            service.QueueURL.Should().Be("testQ URL");
        }

        [Test]
        public void WhenNoQueueNameProvided_ShouldThrowArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(() => FactoryMethod(string.Empty));
            ex.Message.Should().Be("Invalid queue name");
        }

        [Test]
        public async Task WhenReadingMessageStream_ShouldReturnMessagesFromSqsQueue()
        {
            sqsClient.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(new ReceiveMessageResponse()
                     {
                         Messages = new List<Message> {
                             new Message { MD5OfBody = "1" },
                             new Message { MD5OfBody = "2" }
                         }
                     }));

            var msgSeq = 0;
            await foreach (var message in SUT().Object.ReadMessageStream(default))
            {
                message.MD5OfBody.Should().Be($"{++msgSeq}");
            }
        }

        [Test]
        public async Task WhenNoMessagesInQueue_ShouldTakeBreakBeforeContinuingToResumeReadingQueue()
        {
            var sqsService = SUT();
            await foreach (var message in sqsService.Object.ReadMessageStream(default)) ;

            sqsService.Verify(x => x.TakeBreakBeforePollingForMessages(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        [Test]
        public async Task WhenDuplicateMessageDetected_ShouldNotReadThatMessage()
        {
            sqsClient.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(new ReceiveMessageResponse()
                     {
                         Messages = new List<Message> {
                             new Message { MD5OfBody = "duplicate" },
                         }
                     }));

            var sqsService = SUT();
            sqsService.Setup(x => x.IsDuplicate(It.IsAny<Message>())).Returns(true);

            var numOfMsgRead = 0;
            await foreach (var message in sqsService.Object.ReadMessageStream(default))
            {
                numOfMsgRead++;
            };

            numOfMsgRead.Should().Be(0);
        }
    }
}