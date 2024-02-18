using AutoMapper;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SettingsAPI.Common;
using SettingsAPI.EF;
using SettingsAPI.Mappers;
using SettingsAPI.Model.Rest;
using System;

namespace SettingsAPI.Tests.Mapping
{
    public class MemberProfileTests
    {

        [Test]
        public void CommsPreferencesRequest_ShouldMapCorrectly()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MemberProfile>());
            var mapper = config.CreateMapper();

            var source = new CommsPreferencesRequest
            {
                SubscribeSMS = false,
                SubscribeAppNotifications = false,
                SubscribeNewsletters = false
            };
            var dest = mapper.Map<UpdateCommsPreferencesModel>(source, opts => {
                opts.Items[Constant.Mapper.MemberId] = 123;
                opts.Items[Constant.Mapper.PersonId] = null;
            });

            dest.Should().BeEquivalentTo(new UpdateCommsPreferencesModel
            {
                SubscribeSMS = false,
                SubscribeAppNotifications = false,
                SubscribeNewsletters = false,
                PersonId = null,
                MemberId = 123
            });
        }
    }
}
