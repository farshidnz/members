using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests
{
    public class StartupTests
    {
        private class TestState
        {
            public Mock<IConfiguration> configuration;
            public Mock<IServiceCollection> serviceCollection;

            public TestState()
            {
                configuration = new Mock<IConfiguration>();
                serviceCollection = new Mock<IServiceCollection>();
            }
        }

        private Startup SUT(TestState testState)
        {
            return new Startup(testState.configuration.Object);
        }

        [Fact]
        public void ShouldRegisterOpenTelemetry()
        {
            var testState = new TestState();
            SUT(testState).AddOpenTelementry(testState.serviceCollection.Object);
            var x = testState.serviceCollection.Invocations;
            testState.serviceCollection.Verify(sc => sc.Add(It.Is<ServiceDescriptor>(sd =>
                sd.Lifetime == ServiceLifetime.Singleton &&
                sd.ServiceType == typeof(TracerProvider)
            )));
        }
    }
}
