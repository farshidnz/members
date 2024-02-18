using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;
using SettingsAPI.Service;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class TestMemberPaypalAccountService
    {
        /* Common init data */
        private const int MemberId = 10000;
        private const string TokenEndpoint = "https://api.sandbox.paypal.com/v1/oauth2/token";

        private const string UserInfoEndpoint =
            "https://api.sandbox.paypal.com/v1/identity/oauth2/userinfo?schema=paypalv1.1";

        private const string Base64ClientToken = "client token base 64 dummy";
        private const string Code = "dummy code";
        private const string OtherCode = "other dummy code";
        private const string OtherCode1 = "other1 dummy code";
        private const string OtherCode2 = "other2 dummy code";
        private const string RefreshToken = "dummy-refresh-token";
        private const string AccessToken = "dummy-access-token";
        private const string OtherRefreshToken = "other-dummy-refresh-token";
        private const string OtherAccessToken = "other-dummy-access-token";
        private const string OtherAccessToken1 = "other1-dummy-access-token";
        private const string OtherAccessToken2 = "other2-dummy-access-token";
        private const string PaypalClientId = "dummy paypal client id";
        private const string PaypalClientSecret = "dummy paypal client id";

        //Valid data
        private const int OtherMemberId = 100002;
        private const int OtherMemberId1 = 100003;
        private const int OtherMemberId2 = 100004;
        private const string PaypalEmail = "dummy@gmail.com";
        private const string OtherPaypalEmail = "otherdummy@gmail.com";
        private const string OtherPaypalEmail1 = "other1dummy@gmail.com";
        private const string OtherPaypalEmail2 = "other2dummy@gmail.com";
        private const string OtherPaypalEmail3 = "other3dummy@gmail.com";
        private const string RedirectUri = "https://abc.com";
        
        /* Invalid data (false)*/
        private const int FMemberId = 10001;
        private const string FCode = "invalid code";
        private const string FRedirectUri = "abc";


        [Fact]
        public async Task TestGetLinkedPaypalAccount()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberPaypalAccountService = InitMemberPaypalAccountService(shopGoContext, readOnlyShopGoContext);

            //Member paypal account not found
            await Assert.ThrowsAsync<MemberPaypalException>(() =>
                memberPaypalAccountService.GetLinkedPaypalAccount(FMemberId));

            //Paypal email not found
            await Assert.ThrowsAsync<MemberPaypalException>(() =>
                memberPaypalAccountService.GetLinkedPaypalAccount(OtherMemberId));
            
            //Paypal unverify
            await Assert.ThrowsAsync<MemberPaypalException>(() =>
                memberPaypalAccountService.GetLinkedPaypalAccount(OtherMemberId2));

            //Valid
            try
            {
                await memberPaypalAccountService.GetLinkedPaypalAccount(MemberId);
            }
            catch (MemberPaypalException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public async Task TestUnlinkMemberPaypalAccount()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberPaypalAccountService = InitMemberPaypalAccountService(shopGoContext, readOnlyShopGoContext);

            //Member paypal account not found
            await Assert.ThrowsAsync<MemberPaypalException>(() =>
                memberPaypalAccountService.UnlinkMemberPaypalAccount(FMemberId));

            //Paypal email not found
            await Assert.ThrowsAsync<MemberPaypalException>(() =>
                memberPaypalAccountService.UnlinkMemberPaypalAccount(OtherMemberId));

            //Valid
            await memberPaypalAccountService.UnlinkMemberPaypalAccount(MemberId);
            shopGoContext.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task TestLinkMemberPaypalAccount()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberPaypalAccountService = InitMemberPaypalAccountService(shopGoContext, readOnlyShopGoContext);

            // 401 unauthorized code
            await Assert.ThrowsAsync<PaypalAuthorizationCodeUnauthorizedException>(() =>
                memberPaypalAccountService.LinkMemberPaypalAccount(It.IsAny<int>(), FCode
                ));

            //Duplicated paypal email (paypal email used by other account)
            await Assert.ThrowsAsync<DuplicatePaypalAccountException>(() =>
                memberPaypalAccountService.LinkMemberPaypalAccount(
                    MemberId, OtherCode1));
            
            //Paypal not verified
            await Assert.ThrowsAsync<PaypalAccountHasNotBeenVerifiedException>(() =>
                memberPaypalAccountService.LinkMemberPaypalAccount(
                    MemberId, OtherCode2));
            
            //Valid
            
            await memberPaypalAccountService.LinkMemberPaypalAccount(MemberId, Code);
            shopGoContext.Verify(s=>s.SaveChangesAsync(CancellationToken.None), Times.Exactly(4));
        }

        [Fact]
        public void TestGetPaypalConnectUrl()
        {
            var shopGoContext = InitShopGoContext();
            var readOnlyShopGoContext = InitReadOnlyShopGoContext();
            var memberPaypalAccountService = InitMemberPaypalAccountService(shopGoContext, readOnlyShopGoContext);

            //Invalid uri
            Assert.Throws<InvalidUriException>(() => memberPaypalAccountService.GetPaypalConnectUrl(
                FRedirectUri, It.IsAny<string>()));

            //Valid
            Assert.NotNull(memberPaypalAccountService.GetPaypalConnectUrl(RedirectUri, It.IsAny<string>()));
        }

        private static MemberPaypalAccountService InitMemberPaypalAccountService(IMock<ShopGoContext> shopGoContextMock, IMock<ReadOnlyShopGoContext> readOnlyShopGOContextMock)
        {
            var optionsMock = InitOptionsMock();
            var encryptionMock = InitEncryptionServiceMock();
            var paypalApiMock = InitPaypalApiServiceMock();
            var validationMock = InitValidationServiceMock();

            var service = new MemberPaypalAccountService(shopGoContextMock.Object, readOnlyShopGOContextMock.Object, optionsMock.Object,
                encryptionMock.Object, paypalApiMock.Object, validationMock.Object, new Mock<ITimeService>().Object);

            return service;
        }

        private static Mock<ShopGoContext> InitShopGoContext()
        {
            var memberPaypalAccounts = InitMemberPaypalAccounts();

            var memberPaymentMethodHistories = InitMemberPaymentMethodHistories();

            var optionsBuilder = new DbContextOptionsBuilder<ShopGoContext>();
            var contextMock = new Mock<ShopGoContext>(optionsBuilder.Options);
            contextMock.Setup(p => p.MemberPaypalAccount).ReturnsDbSet(memberPaypalAccounts);
            contextMock.Setup(p => p.MemberPaymentMethodHistory).ReturnsDbSet(memberPaymentMethodHistories);

            return contextMock;
        }

        private static List<MemberPaymentMethodHistory> InitMemberPaymentMethodHistories()
        {
            return new List<MemberPaymentMethodHistory>
            {
                new MemberPaymentMethodHistory
                {
                    MemberId = MemberId
                }
            };
        }

        private static List<MemberPaypalAccount> InitMemberPaypalAccounts()
        {
            return new List<MemberPaypalAccount>
            {
                new MemberPaypalAccount
                {
                    MemberId = MemberId,
                    StatusId = StatusType.Active.GetHashCode(),
                    PaypalEmail = PaypalEmail,
                    VerifiedAccount = true
                },
                new MemberPaypalAccount
                {
                    MemberId = OtherMemberId,
                    StatusId = StatusType.Active.GetHashCode(),
                    PaypalEmail = null
                },
                new MemberPaypalAccount
                {
                    MemberId = OtherMemberId1,
                    StatusId = StatusType.Active.GetHashCode(),
                    PaypalEmail = OtherPaypalEmail1
                },
                new MemberPaypalAccount
                {
                    MemberId = OtherMemberId2,
                    StatusId = StatusType.Active.GetHashCode(),
                    PaypalEmail = OtherPaypalEmail1
                },
                new MemberPaypalAccount
                {
                    MemberId = OtherMemberId2,
                    StatusId = StatusType.Active.GetHashCode(),
                    PaypalEmail = OtherPaypalEmail3,
                    VerifiedAccount = null
                }
            };
        }

        private static Mock<ReadOnlyShopGoContext> InitReadOnlyShopGoContext()
        {
            var memberPaypalAccounts = InitMemberPaypalAccounts();

            var memberPaymentMethodHistories = InitMemberPaymentMethodHistories();

            var optionsBuilder = new DbContextOptionsBuilder<ReadOnlyShopGoContext>();
            var contextMock = new Mock<ReadOnlyShopGoContext>(optionsBuilder.Options);
            contextMock.Setup(p => p.MemberPaypalAccount).ReturnsDbSet(memberPaypalAccounts);
            contextMock.Setup(p => p.MemberPaymentMethodHistory).ReturnsDbSet(memberPaymentMethodHistories);

            return contextMock;
        }

        private static Mock<IOptions<Settings>> InitOptionsMock()
        {
            var settings = new Settings
            {
                PaypalClientId = PaypalClientId,
                PaypalClientSecret = PaypalClientSecret,
                PaypalTokenService = TokenEndpoint,
                PaypalUserInfo = UserInfoEndpoint
            };
            var optionsMock = new Mock<IOptions<Settings>>();

            optionsMock.Setup(s => s.Value).Returns(settings);

            return optionsMock;
        }

        private static Mock<IEncryptionService> InitEncryptionServiceMock()
        {
            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(e => e.Base64Encode(PaypalClientId, PaypalClientSecret))
                .Returns(Base64ClientToken);

            return encryptionServiceMock;
        }

        private static Mock<IValidationService> InitValidationServiceMock()
        {
            var validationServiceMock= new Mock<IValidationService>();

            validationServiceMock.Setup(v=>v.ValidateUri(FRedirectUri)).Throws(
                new InvalidUriException());
            validationServiceMock.Setup(v => v.ValidateUri(RedirectUri));

            return validationServiceMock;

        }

        private static Mock<IPaypalApiService> InitPaypalApiServiceMock()
        {
            var paypalApiServiceMock = new Mock<IPaypalApiService>();

            var tokenHeader = new AuthenticationHeaderValue("Basic", Base64ClientToken);

            var paypalAuthorizationResponse = new PaypalAuthorizationResponse
            {
                RefreshToken = RefreshToken,
                AccessToken = AccessToken
            };
            
            var otherPaypalAuthorizationResponse = new PaypalAuthorizationResponse
            {
                RefreshToken = OtherRefreshToken,
                AccessToken = OtherAccessToken
            };
            
            var other1PaypalAuthorizationResponse = new PaypalAuthorizationResponse
            {
                RefreshToken = OtherRefreshToken,
                AccessToken = OtherAccessToken1
            };
            
            var other2PaypalAuthorizationResponse = new PaypalAuthorizationResponse
            {
                RefreshToken = OtherRefreshToken,
                AccessToken = OtherAccessToken2
            };
            
            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalAuthorizationResponse>(
                    HttpMethod.Post, TokenEndpoint, tokenHeader, $"grant_type=authorization_code&code={Code}"))
                .Returns(Task.FromResult(paypalAuthorizationResponse));
            

            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalAuthorizationResponse>(
                HttpMethod.Post, TokenEndpoint, tokenHeader, $"grant_type=authorization_code&code={FCode}")).Throws(
                new PaypalAuthorizationCodeUnauthorizedException("dummy"));
            
            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalAuthorizationResponse>(
                    HttpMethod.Post, TokenEndpoint, tokenHeader, $"grant_type=authorization_code&code={OtherCode}"))
                .Returns(Task.FromResult(otherPaypalAuthorizationResponse));
            
            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalAuthorizationResponse>(
                    HttpMethod.Post, TokenEndpoint, tokenHeader, $"grant_type=authorization_code&code={OtherCode1}"))
                .Returns(Task.FromResult(other1PaypalAuthorizationResponse));
            
            
            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalAuthorizationResponse>(
                    HttpMethod.Post, TokenEndpoint, tokenHeader, $"grant_type=authorization_code&code={OtherCode2}"))
                .Returns(Task.FromResult(other2PaypalAuthorizationResponse));
            
            var userInfoHeader1 = new AuthenticationHeaderValue("Bearer", AccessToken);
            var userInfoHeader2 = new AuthenticationHeaderValue("Bearer", OtherAccessToken);
            var userInfoHeader3 = new AuthenticationHeaderValue("Bearer", OtherAccessToken1);
            var userInfoHeader4 = new AuthenticationHeaderValue("Bearer", OtherAccessToken2);

            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalUserInfoResponse>(
                    HttpMethod.Get, UserInfoEndpoint, userInfoHeader1, null))
                .Returns(Task.FromResult(new PaypalUserInfoResponse
                {
                    VerifiedAccount = true,
                    Emails = new List<PaypalUserInfoResponse.Email>
                    {
                        new PaypalUserInfoResponse.Email
                        {
                            Value = PaypalEmail,
                            Primary = true
                        }
                    }
                }));

            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalUserInfoResponse>(
                    HttpMethod.Get, UserInfoEndpoint, userInfoHeader2, null))
                .Returns(Task.FromResult(new PaypalUserInfoResponse
                {
                    VerifiedAccount = true,
                    Emails = new List<PaypalUserInfoResponse.Email>
                    {
                        new PaypalUserInfoResponse.Email
                        {
                            Value = OtherPaypalEmail,
                            Primary = true
                        }
                    }
                }));

            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalUserInfoResponse>(
                    HttpMethod.Get, UserInfoEndpoint, userInfoHeader3, null))
                .Returns(Task.FromResult(new PaypalUserInfoResponse
                {
                    VerifiedAccount = true,
                    Emails = new List<PaypalUserInfoResponse.Email>
                    {
                        new PaypalUserInfoResponse.Email
                        {
                            Value = OtherPaypalEmail1,
                            Primary = true
                        }
                    }
                }));
            
            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalUserInfoResponse>(
                    HttpMethod.Get, UserInfoEndpoint, userInfoHeader3, null))
                .Returns(Task.FromResult(new PaypalUserInfoResponse
                {
                    VerifiedAccount = true,
                    Emails = new List<PaypalUserInfoResponse.Email>
                    {
                        new PaypalUserInfoResponse.Email
                        {
                            Value = OtherPaypalEmail1,
                            Primary = true
                        }
                    }
                }));
            
            paypalApiServiceMock.Setup(p => p.ExecuteAsyncCallApi<PaypalUserInfoResponse>(
                    HttpMethod.Get, UserInfoEndpoint, userInfoHeader4, null))
                .Returns(Task.FromResult(new PaypalUserInfoResponse
                {
                    VerifiedAccount = false,
                    Emails = new List<PaypalUserInfoResponse.Email>
                    {
                        new PaypalUserInfoResponse.Email
                        {
                            Value = OtherPaypalEmail2,
                            Primary = true
                        }
                    }
                }));
            
            return paypalApiServiceMock;
        }
    }
}