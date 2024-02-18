using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;

namespace SettingsAPI.Service
{
    public class MemberRedeemService : IMemberRedeemService
    {
        private readonly ShopGoContext _context;
        private readonly IMemberPaypalAccountService _memberPaypalAccountService;
        private readonly IMemberBalanceService _memberBalanceService;
        private readonly ITransactionService _transactionService;
        private readonly IValidationService _validationService;
        private readonly IEmailService _emailService;
        private readonly IMemberBankAccountService _memberBankAccountService;
        private readonly IMemberService _memberService;


        public MemberRedeemService(ShopGoContext context, IMemberPaypalAccountService memberPaypalAccountService,
            IMemberBalanceService memberBalanceService, ITransactionService transactionService,
            IValidationService validationService, IEmailService emailService,
            IMemberBankAccountService memberBankAccountService, IMemberService memberService)
        {
            _context = context;
            _memberPaypalAccountService = memberPaypalAccountService;
            _memberBalanceService = memberBalanceService;
            _transactionService = transactionService;
            _validationService = validationService;
            _emailService = emailService;
            _memberBankAccountService = memberBankAccountService;
            _memberService = memberService;
        }

        internal virtual DateTime GetDateTime()
        {
            return DateTime.UtcNow;
        }

        private string CreateWithdrawalId(int? personId, int memberId)
        {
            var theDateString = GetDateTime().ToString("yyyyMMddHHmmss");
            return $"{personId ?? memberId}-{theDateString}";
        }

        private decimal RoundDown2DecimalPlaces(decimal theAmount)
        {
            var multiplyBy = 100;

            return Math.Floor(theAmount * multiplyBy) / multiplyBy;
        }

        private List<MemberAmountToRedeem> GetMemberAmountsToRedeem(IOrderedEnumerable<MembershipAvailableBalance> membershipBalances, decimal amount)
        {
            var result = new List<MemberAmountToRedeem>();

            var totalAmountRedeemed = 0m;
            foreach (var currentMembershipBalance in membershipBalances)
            {
                var remainingAmount = amount - totalAmountRedeemed;

                if (remainingAmount == 0)
                    break;

                var amountToRedeem = 0m;
                if (currentMembershipBalance.AvailableBalance <= remainingAmount)
                {
                    amountToRedeem = RoundDown2DecimalPlaces((currentMembershipBalance.AvailableBalance ?? 0));
                }
                else
                {
                    amountToRedeem = remainingAmount;
                }

                result.Add(new MemberAmountToRedeem() { MemberId = currentMembershipBalance.MemberId, AmountToRedeem = amountToRedeem });

                totalAmountRedeemed += amountToRedeem;
            }

            return result;
        }

        public async Task Withdraw(int memberId, decimal amount, string paymentMethod, string mobileOtp)
        {
            //Verify
            var membershipInfo = await _memberService.GetMembershipInfo(memberId);
            var personId = membershipInfo?.Items.FirstOrDefault()?.PersonId;

            _validationService.ValidatePaymentMethod(paymentMethod);

            var memberStore = await ValidateAndGetMemberStore(memberId, _context);

            _validationService.ValidateOtp(memberStore.Mobile, mobileOtp, memberStore.Email);

            var membershipBalances = (await ValidateMembershipInfoBalanceStore(membershipInfo, amount))
                .OrderBy(m => m.MemberId);

            await ValidateConditionAllowMembershipInfoRedeem(membershipInfo);

            if (PaymentMethod.PayPal.ToString().Equals(paymentMethod))
                await ValidateMemberPaypalAccount(memberId);

            var withdrawalId = CreateWithdrawalId(personId, memberId);

            var memberAmountsToRedeem = GetMemberAmountsToRedeem(membershipBalances, amount);

            foreach (var currentMemberAmountToRedeem in memberAmountsToRedeem)
            {
                var newMemberRedeem = new MemberRedeem
                {
                    MemberId = currentMemberAmountToRedeem.MemberId,
                    PaymentStatusId = PaymentStatusEnum.Pending.GetHashCode(),
                    AmountRequested = currentMemberAmountToRedeem.AmountToRedeem,
                    DateRequestedUtc = DateTime.UtcNow,
                    DateRequested = DateTime.Now,
                    WithdrawalId = withdrawalId,
                    IsPartial = memberAmountsToRedeem.Count() > 1
                };

                if (PaymentMethod.Bank.ToString().Equals(paymentMethod))
                {
                    var memberBankAccount = await _memberBankAccountService.GetBankAccount(memberId);
                    var reference = memberBankAccount.AccountNumber;

                    const PaymentMethod paymentMethodEnum = PaymentMethod.Bank;
                    newMemberRedeem.PaymentMethodDetail = JsonConvert.SerializeObject(memberBankAccount);
                    newMemberRedeem.PaymentMethodReference = reference;
                    newMemberRedeem.PaymentMethodId = paymentMethodEnum.GetHashCode();
                }

                else if (PaymentMethod.PayPal.ToString().Equals(paymentMethod))
                {
                    var paypalAccount = await _memberPaypalAccountService.GetActiveMemberPaypalAccount(memberId);
                    var reference = paypalAccount.PaypalEmail;

                    const PaymentMethod paymentMethodEnum = PaymentMethod.PayPal;
                    newMemberRedeem.PaymentMethodDetail = JsonConvert.SerializeObject(new { PaypalEmail = reference });
                    newMemberRedeem.PaymentMethodReference = reference;
                    newMemberRedeem.PaymentMethodId = paymentMethodEnum.GetHashCode();
                }

                var objToLog = new
                {
                    MemberId = newMemberRedeem.MemberId,
                    AmountRequested = newMemberRedeem.AmountRequested,
                    DateRequested = newMemberRedeem.DateRequested,
                    DateRequestedUtc = newMemberRedeem.DateRequestedUtc,
                    AvailableBalance = membershipBalances.Sum(bal => bal.AvailableBalance)
                };
                LambdaLogger.Log($"Withdrawal request: {JsonConvert.SerializeObject(objToLog)}");

                await _context.MemberRedeem.AddAsync(newMemberRedeem);
                await _context.SaveChangesAsync();
            }

            //Send mail
            await _emailService.SendEmailAfterWithdrawSuccess(memberStore.Email, memberStore.FirstName, RoundDown2DecimalPlaces(amount), paymentMethod);
        }

        private static async Task<Member> ValidateAndGetMemberStore(int memberId, ShopGoContext context)
        {
            var memberStore = await context.Member.Where(m =>
                    m.MemberId == memberId).AsNoTracking()
                .FirstOrDefaultAsync();

            if (memberStore == null)
                throw new MemberNotFoundException();

            if (!memberStore.IsValidated)
                throw new MemberNoAvailableBalanceException();

            return memberStore;
        }

        private async Task<List<MembershipAvailableBalance>> ValidateMembershipInfoBalanceStore(MembershipDetail membershipDetail, decimal amount)
        {
            if (membershipDetail == null || membershipDetail.Items == null)
                throw new ArgumentNullException("Membership details not defined");

            var memberBalances = await _memberBalanceService.GetBalanceViews(membershipDetail.Items.Select(m => m.MemberId).ToArray(), useCache: false);

            if (memberBalances.Any(m => m.AvailableBalance == null))
                throw new MemberNoAvailableBalanceException();

            var totalBalance = memberBalances.Sum(m => m.AvailableBalance.Value);
            _validationService.ValidateAmount(amount, totalBalance);

            return memberBalances.Where(m => (m.AvailableBalance ?? 0) != 0)
                                .Select(m => new MembershipAvailableBalance() { MemberId = m.MemberID, AvailableBalance = m.AvailableBalance })
                                .ToList();
        }

        private async Task<bool> ValidateConditionAllowMemberRedeem(int memberId)
        {
            var hasMemberRedeemed = await HasMemberRedeemed(memberId);

            if (!hasMemberRedeemed)
            {
                return await _transactionService.HasApprovedPurchases(memberId);
            }

            return true;
        }

        private async Task ValidateConditionAllowMembershipInfoRedeem(MembershipDetail membershipDetail)
        {
            var validation = false;
            foreach (var currentMembership in membershipDetail.Items)
            {
                validation = validation || await ValidateConditionAllowMemberRedeem(currentMembership.MemberId);
            }

            if (!validation)
                throw new MemberNotRedeemException();
        }

        private async Task<bool> HasMemberRedeemed(int memberId)
        {
            var hasRecords = await _context.MemberRedeem.AnyAsync(x =>
                x.MemberId == memberId && x.PaymentStatusId == PaymentStatusEnum.Paid.GetHashCode());

            return hasRecords;
        }

        private async Task ValidateMemberPaypalAccount(int memberId)
        {
            var paypalAccount = await _memberPaypalAccountService.GetActiveMemberPaypalAccount(memberId);

            if (paypalAccount.VerifiedAccount != true)
                throw new PaypalAccountHasNotBeenVerifiedException(AppMessage.PaypalAccountNotVerify);
        }
    }
}