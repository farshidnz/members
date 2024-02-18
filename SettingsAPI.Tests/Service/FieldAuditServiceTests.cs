using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using SettingsAPI.Data;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class FieldAuditServiceTests
    {
        private class TestState 
        {
            public FieldAuditService FieldAuditService { get; }

            public TestState()
            {
                FieldAuditService = new FieldAuditService();
            }
        }

        
        [Fact]
        public void GetUpdateMemberFieldAudits_ShouldReturnFieldAudits_ForChangedProperties()
        {
            var state = new TestState();
            var updatedMember = new EF.Member()
            {
                FirstName = "FName",
                LastName = "LName-old",
                PostCode = "2118",
                Mobile = "0449912346",
                DateOfBirth = new DateTime(2011,01,01)
            };

            var existingMember = new EF.Member()
            {
                FirstName = "FName-old",
                LastName = "LName-old",
                PostCode = "2117",
                Mobile = "0449912345",
                DateOfBirth = new DateTime(2010, 01, 01)
            };

            var result = state.FieldAuditService.GetUpdateMemberFieldAudits(updatedMember, existingMember);
            Console.WriteLine(JsonConvert.SerializeObject(result));
            var postCode = result[1];
            postCode.ToValue.Should().BeEquivalentTo("**18");
            var dob = result[2];
            dob.ToValue.Should().BeEquivalentTo("**/**/2011");
            result.Should().HaveCount(3);
        }

        [Theory]
        [InlineData("1234", "**34")]
        [InlineData("123", "")]
        [InlineData("12345", "")]
        public void GetUpdateMemberFieldAudits_ShouldReturnFieldAudits_ForChangedPostCodes(
            string postCode, string expectedPostCode )
        {
            var state = new TestState();
            var updatedMember = new EF.Member()
            {
                FirstName = "FName",
                LastName = "LName",
                PostCode = postCode,
                DateOfBirth = new DateTime(2011, 01, 01)
            };

            var existingMember = new EF.Member()
            {
                FirstName = "FName",
                LastName = "LName",
                PostCode = "2117",
                DateOfBirth = new DateTime(2011, 01, 01)
            };


            var result = state.FieldAuditService.GetUpdateMemberFieldAudits(updatedMember, existingMember);
            Console.WriteLine(JsonConvert.SerializeObject(result));
            var postCodeResult = result[0];
            postCodeResult.ToValue.Should().BeEquivalentTo(expectedPostCode);
        }

        [Fact]
        public void GetUpdateMemberFieldAudits_ShouldReturnFieldAudits_WhenEixistingPostCodeIsNull()
        {
            var state = new TestState();
            var updatedMember = new EF.Member()
            {
                FirstName = "FName",
                LastName = "LName",
                PostCode = "2117",
                DateOfBirth = new DateTime(2011, 01, 01)
            };

            var existingMember = new EF.Member()
            {
                FirstName = "FName",
                LastName = "LName",
                PostCode = null,
                DateOfBirth = new DateTime(2011, 01, 01)
            };


            var result = state.FieldAuditService.GetUpdateMemberFieldAudits(updatedMember, existingMember);
            Console.WriteLine(JsonConvert.SerializeObject(result));
            var postCodeResult = result[0];
            postCodeResult.ToValue.Should().BeEquivalentTo("**17");
        }

        [Theory]
        [InlineData("+61 449900901", "44****901")]
        [InlineData("+64 270000652", "27****652")]
        [InlineData("0449930902", "")]
        public void GetUpdateMemberFieldAudits_ShouldReturnFieldAudits_ForChangedMobile(
            string updatedMobileMember, string expectedMobileNumber)
        {
            var state = new TestState();
            var existingMobileMember = "+61 449900000";

            var result = state.FieldAuditService.GetUpdateMobileFieldAudit(updatedMobileMember, existingMobileMember);
            result.ToValue.Should().BeEquivalentTo(expectedMobileNumber);
        }

        [Fact]
        public void GetUpdateMemberFieldAudits_ShouldReturnFieldAudits_WhenExistingMobileIsNull()
        {
            var state = new TestState();
            
            var result = state.FieldAuditService.GetUpdateMobileFieldAudit(null, "+61 441234567");
            result.ToValue.Should().BeEquivalentTo("");
            result.FromValue.Should().BeEquivalentTo("44****567");
        }

    }
}
