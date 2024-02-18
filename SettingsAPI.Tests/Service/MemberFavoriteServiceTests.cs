using FluentAssertions;
using Moq;
using SettingsAPI.Common;
using SettingsAPI.Model.Dto;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using SettingsAPI.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class MemberFavoriteServiceTests
    {
        private class TestState : EFMockBase
        {
            public MemberFavoriteService MemberFavoriteService { get; }

            public Mock<IFeatureToggleService> FeatureToggleServiceMock { get; }

            public TestState()
            {
                FeatureToggleServiceMock = new Mock<IFeatureToggleService>();
                MemberFavoriteService = new MemberFavoriteService(Context, ReadOnlyContext, new PremiumService(ReadOnlyContext), FeatureToggleServiceMock.Object);
            }
        }

        #region GetFavourites

        [Fact]
        public async Task GetAllFavouriteAsync_ShouldReturnPremiumRates_GivenMerchantViewsForCashrewardsAndAnz()
        {
            var state = new TestState();
            var memberSet = new List<EF.Member>
            {
                new EF.Member { MemberId = 100, ClientId = Constant.Clients.CashRewards, PersonId = 200 },
                new EF.Member { MemberId = 101, ClientId = Constant.Clients.ANZ, PersonId = 200 }
            };
            var cognitoMemberSet = new List<EF.CognitoMember>
            {
                new EF.CognitoMember { MemberId = 100, CognitoId = "11111111-1111-1111-1111-111111111111", PersonId = 200, Member = memberSet.Single(m => m.MemberId == 100) },
                new EF.CognitoMember { MemberId = 101, CognitoId = "11111111-1111-1111-1111-111111111111", PersonId = 200, Member = memberSet.Single(m => m.MemberId == 101) }
            };
            var personSet = new List<EF.Person>
            {
                new EF.Person { PersonId = 200, CognitoId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PremiumStatus = 1, CognitoMember = cognitoMemberSet }
            };
            var merchantViewSet = new List<EF.MerchantView>
            {
                new EF.MerchantView
                {
                    MerchantId = 300,
                    ClientId = Constant.Clients.CashRewards,
                    HyphenatedString = "1001-optical",
                    MerchantName = "1001 Optical",
                    ClientComm = 70,
                    MemberComm = 100,
                    Commission = 15,
                    RegularImageUrl = "//cdn.cashrewards.com/1001-optical.jpg",
                    NetworkId = 1000008,
                    TierCount = 1,
                    IsFlatRate = true
                },
                new EF.MerchantView
                {
                    MerchantId = 300,
                    ClientId = Constant.Clients.ANZ,
                    HyphenatedString = "1001-optical",
                    MerchantName = "1001 Optical",
                    ClientComm = 80,
                    MemberComm = 100,
                    Commission = 15,
                    RegularImageUrl = "//cdn.cashrewards.com/1001-optical.jpg",
                    NetworkId = 1000008,
                    TierCount = 1,
                    IsFlatRate = true
                },
                new EF.MerchantView
                {
                    MerchantId = 301,
                    ClientId = Constant.Clients.CashRewards,
                    HyphenatedString = "amazon-australia",
                    MerchantName = "Amazon Australia",
                    ClientComm = 75,
                    MemberComm = 100,
                    Commission = 16,
                    RegularImageUrl = "//cdn.cashrewards.com/amazon-australia.jpg",
                    NetworkId = 1000057,
                    IsFlatRate = false,
                    TierCount = 3
                },
                new EF.MerchantView
                {
                    MerchantId = 301,
                    ClientId = Constant.Clients.ANZ,
                    HyphenatedString = "amazon-australia",
                    MerchantName = "Amazon Australia",
                    ClientComm = 85,
                    MemberComm = 100,
                    Commission = 16,
                    RegularImageUrl = "//cdn.cashrewards.com/amazon-australia.jpg",
                    NetworkId = 1000057,
                    IsFlatRate = false,
                    TierCount = 3
                }
            };
            var memberFavouriteSet = new List<EF.MemberFavourite>
            {
                new EF.MemberFavourite { MemberId = 100, MerchantId = 300 },
                new EF.MemberFavourite { MemberId = 100, MerchantId = 301 }
            };
            state.SetUpReadOnlyDbContext(x => x.Member, memberSet);
            state.SetUpReadOnlyDbContext(x => x.CognitoMember, cognitoMemberSet);
            state.SetUpReadOnlyDbContext(x => x.Person, personSet);
            state.SetUpReadOnlyDbContext(x => x.MerchantView, merchantViewSet);
            state.SetUpReadOnlyDbContext(x => x.MemberFavourite, memberFavouriteSet);

            var result = await state.MemberFavoriteService.GetAllFavouriteAsync(100, "11111111-1111-1111-1111-111111111111");

            result.Should().BeEquivalentTo(new List<MerchantResult>
            {
                new MerchantResult
                {
                    ClientCommission = 10.5m,
                    ClientCommissionString = "10.5% cashback",
                    HyphenatedString = "1001-optical",
                    IsOnline = true,
                    IsTrueRewards = false,
                    MerchantId = 300,
                    MerchantName = "1001 Optical",
                    NetworkId = 1000008,
                    RegularImageUrl = "//cdn.cashrewards.com/1001-optical.jpg",
                    Premium = null
               },
               new MerchantResult
               {
                    ClientCommission = 12,
                    ClientCommissionString = "Up to 12% cashback",
                    HyphenatedString = "amazon-australia",
                    IsOnline = true,
                    IsTrueRewards = false,
                    MerchantId = 301,
                    MerchantName = "Amazon Australia",
                    NetworkId = 1000057,
                    RegularImageUrl = "//cdn.cashrewards.com/amazon-australia.jpg",
                    Premium = null
                }
            });
        }

        #endregion

        #region AddFavourites

        private static void GivenMerchant1001Optical(TestState state)
        {
            var merchantSet = new List<EF.Merchant>
            {
                new EF.Merchant { MerchantId = 300, HyphenatedString = "1001-optical" }
            };
            var memberFavouriteSet = new List<EF.MemberFavourite>();
            state.SetUpDbContext(x => x.Merchant, merchantSet);
            state.SetUpDbContext(x => x.MemberFavourite, memberFavouriteSet);
        }

        [Fact]
        public async Task AddFavouriteAsync_ShouldAddFavourite_GivenValidMerchant()
        {
            var state = new TestState();
            GivenMerchant1001Optical(state);

            await state.MemberFavoriteService.AddFavouriteAsync(100, new MemberFavouriteRequestMerchant
            {
                MerchantId = 300,
                HyphenatedString = "1001-optical"
            });

            state.ContextMock.Verify(x => x.MemberFavourite.AddRange(It.Is<EF.MemberFavourite[]>(f => f.Count() == 1)), Times.Once());
        }

        [Fact]
        public async Task AddFavouriteAsync_ShouldAddFavourite_GivenMerchantWithNullHyphenatedString_ForBackwardCompatibility()
        {
            var state = new TestState();
            GivenMerchant1001Optical(state);

            await state.MemberFavoriteService.AddFavouriteAsync(100, new MemberFavouriteRequestMerchant
            {
                MerchantId = 300,
                HyphenatedString = null
            });

            state.ContextMock.Verify(x => x.MemberFavourite.AddRange(It.Is<EF.MemberFavourite[]>(f => f.Count() == 1)), Times.Once());
        }

        [Fact]
        public async Task AddFavouriteAsync_ShouldNotAddFavourite_GivenInvalidMerchantId()
        {
            var state = new TestState();
            GivenMerchant1001Optical(state);

            await state.MemberFavoriteService.AddFavouriteAsync(100, new MemberFavouriteRequestMerchant
            {
                MerchantId = 888,
                HyphenatedString = "1001-optical"
            });

            state.ContextMock.Verify(x => x.MemberFavourite.AddRange(It.IsAny<EF.MemberFavourite[]>()), Times.Never());
        }

        [Fact]
        public async Task AddFavouriteAsync_ShouldNotAddFavourite_GivenInvalidHyphenatedString()
        {
            var state = new TestState();
            GivenMerchant1001Optical(state);

            await state.MemberFavoriteService.AddFavouriteAsync(100, new MemberFavouriteRequestMerchant
            {
                MerchantId = 300,
                HyphenatedString = "1001-xxxxx"
            });

            state.ContextMock.Verify(x => x.MemberFavourite.AddRange(It.IsAny<EF.MemberFavourite[]>()), Times.Never());
        }

        #endregion

        #region SetFavourites

        private static void GivenMerchant1001OpticalAndAmazonAustralia(TestState state)
        {
            var merchantSet = new List<EF.Merchant>
            {
                new EF.Merchant { MerchantId = 300, HyphenatedString = "1001-optical" },
                new EF.Merchant { MerchantId = 301, HyphenatedString = "amazon-australia" }
            };
            var memberFavouriteSet = new List<EF.MemberFavourite>();
            state.SetUpDbContext(x => x.Merchant, merchantSet);
            state.SetUpDbContext(x => x.MemberFavourite, memberFavouriteSet);
        }

        [Fact]
        public async Task SetFavouriteAsync_ShouldSetFavourites_GivenValidMerchants()
        {
            var state = new TestState();
            GivenMerchant1001OpticalAndAmazonAustralia(state);

            await state.MemberFavoriteService.SetFavouritesAsync(100, new MemberFavouriteRequest
            {
                Merchants = new List<MemberFavouriteRequestMerchant>
                {
                    new MemberFavouriteRequestMerchant { MerchantId = 300, HyphenatedString = "1001-optical" },
                    new MemberFavouriteRequestMerchant { MerchantId = 301, HyphenatedString = "amazon-australia" }
                }
            });

            state.ContextMock.Verify(x => x.MemberFavourite.AddRange(It.Is<EF.MemberFavourite[]>(f => f.Count() == 2)), Times.Once());
        }

        [Fact]
        public async Task SetFavouriteAsync_ShouldOnlySetValidFavourites_GivenValidAndInvalidMerchantIds()
        {
            var state = new TestState();
            GivenMerchant1001OpticalAndAmazonAustralia(state);

            await state.MemberFavoriteService.SetFavouritesAsync(100, new MemberFavouriteRequest
            {
                Merchants = new List<MemberFavouriteRequestMerchant>
                {
                    new MemberFavouriteRequestMerchant { MerchantId = 300, HyphenatedString = "1001-optical" },
                    new MemberFavouriteRequestMerchant { MerchantId = 888, HyphenatedString = "amazon-australia" }
                }
            });

            state.ContextMock.Verify(x => x.MemberFavourite.AddRange(It.Is<EF.MemberFavourite[]>(f => f.Count() == 1)), Times.Once());
        }


        [Fact]
        public async Task SetFavouriteAsync_ShouldOnlySetValidFavourites_GivenValidAndInvalidHyphenatedStrings()
        {
            var state = new TestState();
            GivenMerchant1001OpticalAndAmazonAustralia(state);

            await state.MemberFavoriteService.SetFavouritesAsync(100, new MemberFavouriteRequest
            {
                Merchants = new List<MemberFavouriteRequestMerchant>
                {
                    new MemberFavouriteRequestMerchant { MerchantId = 300, HyphenatedString = "1001-xxxxxx" },
                    new MemberFavouriteRequestMerchant { MerchantId = 301, HyphenatedString = "amazon-australia" }
                }
            });

            state.ContextMock.Verify(x => x.MemberFavourite.AddRange(It.Is<EF.MemberFavourite[]>(f => f.Count() == 1)), Times.Once());
        }

        #endregion

        [Fact]
        public async Task GetAllFavouriteAsync_ShouldNotReturnMerchant_IFPaused()
        {
            var state = new TestState();
            state.FeatureToggleServiceMock.Setup(p => p.IsEnable(It.IsAny<string>())).Returns(true);
            var memberSet = new List<EF.Member>
            {
                new EF.Member { MemberId = 100, ClientId = Constant.Clients.CashRewards, PersonId = 200 },
                new EF.Member { MemberId = 101, ClientId = Constant.Clients.ANZ, PersonId = 200 }
            };
            var cognitoMemberSet = new List<EF.CognitoMember>
            {
                new EF.CognitoMember { MemberId = 100, CognitoId = "11111111-1111-1111-1111-111111111111", PersonId = 200, Member = memberSet.Single(m => m.MemberId == 100) },
                new EF.CognitoMember { MemberId = 101, CognitoId = "11111111-1111-1111-1111-111111111111", PersonId = 200, Member = memberSet.Single(m => m.MemberId == 101) }
            };
            var personSet = new List<EF.Person>
            {
                new EF.Person { PersonId = 200, CognitoId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PremiumStatus = 1, CognitoMember = cognitoMemberSet }
            };
            var merchantViewSet = new List<EF.MerchantView>
            {
                new EF.MerchantView
                {
                    MerchantId = 300,
                    ClientId = Constant.Clients.CashRewards,
                    HyphenatedString = "1001-optical",
                    MerchantName = "1001 Optical",
                    ClientComm = 70,
                    MemberComm = 100,
                    Commission = 15,
                    RegularImageUrl = "//cdn.cashrewards.com/1001-optical.jpg",
                    NetworkId = 1000008,
                    TierCount = 1,
                    IsFlatRate = true
                },
                new EF.MerchantView
                {
                    MerchantId = 300,
                    ClientId = Constant.Clients.ANZ,
                    HyphenatedString = "1001-optical",
                    MerchantName = "1001 Optical",
                    ClientComm = 80,
                    MemberComm = 100,
                    Commission = 15,
                    RegularImageUrl = "//cdn.cashrewards.com/1001-optical.jpg",
                    NetworkId = 1000008,
                    TierCount = 1,
                    IsFlatRate = true
                },
                new EF.MerchantView
                {
                    MerchantId = 301,
                    ClientId = Constant.Clients.CashRewards,
                    HyphenatedString = "amazon-australia",
                    MerchantName = "Amazon Australia",
                    ClientComm = 75,
                    MemberComm = 100,
                    Commission = 16,
                    RegularImageUrl = "//cdn.cashrewards.com/amazon-australia.jpg",
                    NetworkId = 1000057,
                    IsFlatRate = false,
                    TierCount = 3,
                    IsPaused = true
                },
                new EF.MerchantView
                {
                    MerchantId = 301,
                    ClientId = Constant.Clients.ANZ,
                    HyphenatedString = "amazon-australia",
                    MerchantName = "Amazon Australia",
                    ClientComm = 85,
                    MemberComm = 100,
                    Commission = 16,
                    RegularImageUrl = "//cdn.cashrewards.com/amazon-australia.jpg",
                    NetworkId = 1000057,
                    IsFlatRate = false,
                    TierCount = 3,
                     IsPaused = true
                }
            };
            var memberFavouriteSet = new List<EF.MemberFavourite>
            {
                new EF.MemberFavourite { MemberId = 100, MerchantId = 300 },
                new EF.MemberFavourite { MemberId = 100, MerchantId = 301 }
            };
            state.SetUpReadOnlyDbContext(x => x.Member, memberSet);
            state.SetUpReadOnlyDbContext(x => x.CognitoMember, cognitoMemberSet);
            state.SetUpReadOnlyDbContext(x => x.Person, personSet);
            state.SetUpReadOnlyDbContext(x => x.MerchantView, merchantViewSet);
            state.SetUpReadOnlyDbContext(x => x.MemberFavourite, memberFavouriteSet);

            var result = await state.MemberFavoriteService.GetAllFavouriteAsync(100, "11111111-1111-1111-1111-111111111111");

            result.Should().BeEquivalentTo(new List<MerchantResult>
            {
                new MerchantResult
                {
                    ClientCommission = 10.5m,
                    ClientCommissionString = "10.5% cashback",
                    HyphenatedString = "1001-optical",
                    IsOnline = true,
                    IsTrueRewards = false,
                    MerchantId = 300,
                    MerchantName = "1001 Optical",
                    NetworkId = 1000008,
                    RegularImageUrl = "//cdn.cashrewards.com/1001-optical.jpg",
                    Premium = null
               },
               new MerchantResult
               {
                    ClientCommission = 0,
                    ClientCommissionString ="No current offers",
                    HyphenatedString = "amazon-australia",
                    IsOnline = true,
                    IsTrueRewards = false,
                    MerchantId = 301,
                    MerchantName = "Amazon Australia",
                    NetworkId = 1000057,
                    RegularImageUrl = "//cdn.cashrewards.com/amazon-australia.jpg",
                    Premium = null
                }
            });
        }
    }
}
