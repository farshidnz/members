using FluentAssertions;
using Moq;
using SettingsAPI.Service;
using System;
using System.Collections.Generic;
using System.Text;
using Unleash;
using Xunit;

namespace SettingsAPI.Tests.Service
{


    public class UnleashFeatureToggleServiceTests
    {
        private Mock<IUnleash> _unleashMock;
        public UnleashFeatureToggleServiceTests()
        {
            _unleashMock = new Mock<IUnleash>();
        }


        [Fact]
        public void IsEnable_UnleashApi_Called()
        {
            _unleashMock.Setup(p => p.IsEnabled(It.IsAny<string>())).Returns(true);

            var featureToggle = new UnleashFeatureToggleService(_unleashMock.Object);

            var result = featureToggle.IsEnable("feature1");

            result.Should().BeTrue();
            _unleashMock.Verify(p => p.IsEnabled(It.IsAny<string>()), Times.Once);
        }
    }
}
