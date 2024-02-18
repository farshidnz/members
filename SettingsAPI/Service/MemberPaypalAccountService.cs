using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;

namespace SettingsAPI.Service
{
    public class MemberPaypalAccountService : IMemberPaypalAccountService
    {
        private readonly ShopGoContext _context;
        private readonly ReadOnlyShopGoContext _readOnlyContext;
        private readonly IOptions<Settings> _settings;
        private readonly IEncryptionService _encryptionService;
        private readonly IPaypalApiService _paypalApiService;
        private readonly IValidationService _validationService;
        private readonly ITimeService _timeService;

        public MemberPaypalAccountService(ShopGoContext context, ReadOnlyShopGoContext readOnlyContext, IOptions<Settings> settings,
            IEncryptionService encryptionService, IPaypalApiService paypalApiService,
            IValidationService validationService,
            ITimeService timeService)
        {
            _context = context;
            _readOnlyContext = readOnlyContext;
            _settings = settings;
            _encryptionService = encryptionService;
            _paypalApiService = paypalApiService;
            _validationService = validationService;
            _timeService = timeService;
        }

        public async Task<MemberPaypalAccount> GetActiveMemberPaypalAccount(int memberId)
        {
            var memberPaypalAccount = await _context.MemberPaypalAccount
                .Where(m => m.MemberId == memberId && m.StatusId == StatusType.Active.GetHashCode())
                .FirstOrDefaultAsync();

            if (memberPaypalAccount == null)
                throw new MemberPaypalException(string.Format(AppMessage.ObjectNotFound, "Member paypal"));

            return memberPaypalAccount;
        }

        public async Task UpdateMemberPaypalAccount(int memberId, string paypalEmail, string accessToken,
            string refreshToken, bool verifiedAccount)
        {
            var activeAccount = await _context.MemberPaypalAccount.Where(acct =>
                    acct.MemberId == memberId && acct.PaypalEmail == paypalEmail &&
                    acct.StatusId == StatusType.Active.GetHashCode())
                .FirstOrDefaultAsync();

            if (activeAccount != null)
            {
                activeAccount.VerifiedAccount = verifiedAccount;
                activeAccount.RefreshToken = refreshToken;
                await _context.SaveChangesAsync();
            }
        }


        public async Task LinkMemberPaypalAccount(int memberId, string code)
        {
            //Get refresh token and access token
            var authorizationResponse = await _paypalApiService.ExecuteAsyncCallApi<PaypalAuthorizationResponse>(
                HttpMethod.Post,
                _settings.Value.PaypalTokenService,
                new AuthenticationHeaderValue("Basic",
                    _encryptionService.Base64Encode(_settings.Value.PaypalClientId,
                        _settings.Value.PaypalClientSecret)),
                $"grant_type=authorization_code&code={code}").ConfigureAwait(false);

            var accessToken = authorizationResponse.AccessToken;
            var refreshToken = authorizationResponse.RefreshToken;

            //Get verified account and email
            var userInfoResponse = await _paypalApiService.ExecuteAsyncCallApi<PaypalUserInfoResponse>(
                    HttpMethod.Get,
                    _settings.Value.PaypalUserInfo,
                    new AuthenticationHeaderValue("Bearer", accessToken))
                .ConfigureAwait(false);

            bool verifiedAccount = userInfoResponse.VerifiedAccount;
            var emails = userInfoResponse.Emails;
            var paypalEmail = emails.FirstOrDefault(e => e.Primary)?.Value;

            //Validate paypal email before save
            await ValidatePaypalAccountExisting(memberId, paypalEmail);

            //Change old accounts to inactive
            var memberPaypalAccountStoreActives = await _context.MemberPaypalAccount.Where(acct =>
                acct.MemberId == memberId && acct.StatusId == StatusType.Active.GetHashCode()).ToListAsync();

            memberPaypalAccountStoreActives.ForEach(account => account.StatusId = StatusType.Inactive.GetHashCode());
            memberPaypalAccountStoreActives.ForEach(async x => await AddChangeHistory(x, ChangeAction.Remove));

            // Add new record as active.
            var newPaypalAccount = new MemberPaypalAccount
            {
                MemberId = memberId,
                StatusId = verifiedAccount ? (int)StatusType.Active : (int)StatusType.Unverified,
                DateEnabled = _timeService.Now,
                PaypalEmail = paypalEmail,
                VerifiedAccount = verifiedAccount,
                RefreshToken = refreshToken
            };

            await _context.MemberPaypalAccount.AddAsync(newPaypalAccount);
            await _context.SaveChangesAsync();

            newPaypalAccount = await _context.MemberPaypalAccount.FirstOrDefaultAsync(acct =>
                acct.MemberId == memberId && acct.StatusId == StatusType.Active.GetHashCode());

            if (newPaypalAccount != null)
                await AddChangeHistory(newPaypalAccount, ChangeAction.Add);

            await _context.SaveChangesAsync();

            if (!verifiedAccount)
                throw new PaypalAccountHasNotBeenVerifiedException(Util.MaskEmail(paypalEmail));
        }

        public async Task<LinkedPaypalAccountInfo> GetLinkedPaypalAccount(int memberId)
        {
            var memberPaypalAccount = await GetActiveMemberPaypalAccount(memberId);

            var paypalEmail = memberPaypalAccount.PaypalEmail;
            if (paypalEmail == null)
                throw new MemberPaypalException(string.Format(AppMessage.ObjectNotFound, "Member paypal email"));

            if (memberPaypalAccount.VerifiedAccount != true)
                throw new MemberPaypalException(string.Format(AppMessage.ObjectNotFound,
                    "Member paypal account verified"));

            return new LinkedPaypalAccountInfo
            {
                PaypalEmail = Util.MaskEmail(paypalEmail)
            };
        }

        public async Task UnlinkMemberPaypalAccount(int memberId)
        {
            var memberPaypalAccount = await GetActiveMemberPaypalAccount(memberId);
            var paypalEmail = memberPaypalAccount.PaypalEmail;
            if (paypalEmail == null)
                throw new MemberPaypalException(string.Format(AppMessage.ObjectNotFound, "Member paypal email"));

            memberPaypalAccount.StatusId = StatusType.Inactive.GetHashCode();
            memberPaypalAccount.DateDisabled = _timeService.Now;

            await AddChangeHistory(memberPaypalAccount, ChangeAction.Remove);

            await _context.SaveChangesAsync();
        }

        public PaypalConnectUrlInfo GetPaypalConnectUrl(string redirectUri, string state)
        {
            //Validate
            _validationService.ValidateUri(redirectUri);

            var paypalConnectUrlBuilder = new StringBuilder();
            var paypalConnectUrlEndpoint = _settings.Value.PaypalConnectUrlEndpoint;
            var paypalClientId = _settings.Value.PaypalClientId;
            var paypalScope = _settings.Value.PaypalScope;

            var redirectUrlBuilder = new StringBuilder();
            redirectUrlBuilder.Append(redirectUri);

            var redirectUrlEncode = HttpUtility.UrlEncode(redirectUrlBuilder.ToString());

            var paypalConnectUrl = paypalConnectUrlBuilder.Append(paypalConnectUrlEndpoint)
                .Append("?flowEntry=static").Append("&client_id=").Append(paypalClientId)
                .Append("&response_type=code").Append("&scope=").Append(HttpUtility.UrlEncode(paypalScope))
                .Append("&redirect_uri=")
                .Append(redirectUrlEncode).Append("&state=").Append(state).ToString();

            return new PaypalConnectUrlInfo
            {
                PaypalConnectUrl = paypalConnectUrl
            };
        }

        private async Task AddChangeHistory(MemberPaypalAccount paypalAccount, ChangeAction changeAction)
        {
            var paymentMethodHistory = new MemberPaymentMethodHistory
            {
                MemberId = paypalAccount.MemberId,
                AccountType = PaymentMethod.PayPal.GetHashCode(),
                AccountId = paypalAccount.Id,
                ChangeType = changeAction.GetHashCode(),
                DateChanged = DateTime.Now,
                HashedValue = _encryptionService.EncryptWithSalt(paypalAccount.PaypalEmail, _settings.Value.SaltKey)
            };
            await _context.MemberPaymentMethodHistory.AddAsync(paymentMethodHistory);
        }

        private async Task ValidatePaypalAccountExisting(int memberId, string paypalEmail)
        {
            var existed = await _readOnlyContext.MemberPaypalAccount
                .Where(account => account.MemberId != memberId &&
                                  account.PaypalEmail == paypalEmail)
                .Select(account => account.MemberId)
                .Distinct()
                .CountAsync();

            //one Paypal accont can only link with two different CR accounts
            if (existed > 1)
                throw new DuplicatePaypalAccountException();
        }
    }
}