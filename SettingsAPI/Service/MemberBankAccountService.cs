using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;

namespace SettingsAPI.Service
{
    public class MemberBankAccountService : IMemberBankAccountService
    {
        private readonly IOptions<Settings> _settings;
        private readonly ShopGoContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IValidationService _validationService;

        public MemberBankAccountService(IOptions<Settings> settings, ShopGoContext context, IEncryptionService encryptionService,
            IValidationService validationService)
        {
            _settings = settings;
            _context = context;
            _encryptionService = encryptionService;
            _validationService = validationService;
        }

        public async Task SaveBankAccount(int memberId, string accountName, string bsb, string accountNumber, string mobileOtp)
        {
            //Validate
            _validationService.ValidateAccountName(accountName);
            _validationService.ValidateAccountNumber(accountNumber);
            await _validationService.ValidateBsb(bsb);

            var memberStore = await ValidateAndGetMemberStore(memberId, _context);
            _validationService.ValidateOtp(memberStore.Mobile, mobileOtp, memberStore.Email);
            
            await ValidateAccountExisting(memberId, accountNumber, bsb, _context);

            //Change old accounts to inactive
            var memberBankAccountStoreActives = await _context.MemberBankAccount.Where(acct =>
                acct.MemberId == memberId && acct.Status == (int) StatusType.Active).ToListAsync();
            memberBankAccountStoreActives.ForEach(account => account.Status = StatusType.Inactive.GetHashCode());

            // add new record as active.
            var newBankAccount = new MemberBankAccount
            {
                AccountName = accountName,
                Bsb = bsb,
                AccountNumber = accountNumber,
                MemberId = memberId,
                BankName = string.Empty,
                DateCreated = DateTime.Now,
                Status = (int) StatusType.Active
            };

            await _context.MemberBankAccount.AddAsync(newBankAccount);
            // save change.
            await _context.SaveChangesAsync();

            // Save change history
            memberBankAccountStoreActives.ForEach(async x => await ChangeHistory(x, ChangeAction.Remove, _context));

            newBankAccount = await _context.MemberBankAccount.FirstOrDefaultAsync(acct =>
                acct.MemberId == memberId && acct.Status == (int) StatusType.Active);

            if (newBankAccount != null)
                await ChangeHistory(newBankAccount, ChangeAction.Add, _context);

            await _context.SaveChangesAsync();
        }

        public async Task<MemberBankAccount> GetBankAccount(int memberId)
        {
            var memberBankAccount = await ValidateAndGetMemberBankAccount(memberId, _context);
            return memberBankAccount;
        }

        public async Task<MemberBankAccountInfo> GetBankAccountMasked(int memberId)
        {
            var memberBankAccount = await ValidateAndGetMemberBankAccount(memberId, _context);
            var accountNumber = memberBankAccount.AccountNumber;

            var accountNumberBuilder = new StringBuilder();
            var threeAccountNumberLastSuffix = accountNumber.Substring(accountNumber.Length - 3, 3);
            for (var i = 0; i < accountNumber.Length - 3; i++)
            {
                accountNumberBuilder.Append("*");
            }

            accountNumberBuilder.Append(threeAccountNumberLastSuffix);
            var accountNumberMask = accountNumberBuilder.ToString();

            return new MemberBankAccountInfo
            {
                AccountName = memberBankAccount.AccountName,
                Bsb = Util.MaskAllWords(memberBankAccount.Bsb),
                AccountNumber = accountNumberMask
            };
        }

        public async Task DisconnectBankAccount(int memberId)
        {
            //Change accounts to inactive
            var memberBankAccountStoreActives = await _context.MemberBankAccount.Where(acct =>
                acct.MemberId == memberId && acct.Status == (int) StatusType.Active).ToListAsync();

            if (memberBankAccountStoreActives.Count == 0)
                throw new MemberBankAccountNotFoundException();

            memberBankAccountStoreActives.ForEach(account => account.Status = StatusType.Inactive.GetHashCode());

            await _context.SaveChangesAsync();

            // Save change history
            memberBankAccountStoreActives.ForEach(async x => await ChangeHistory(x, ChangeAction.Remove, _context));

            await _context.SaveChangesAsync();
        }


        private async Task ChangeHistory(MemberBankAccount bankAccount, ChangeAction changeType, ShopGoContext context)
        {
            var paymentMethodHistory = new MemberPaymentMethodHistory
            {
                MemberId = bankAccount.MemberId,
                AccountType = (int) PaymentMethod.Bank,
                AccountId = bankAccount.BankAccountId,
                ChangeType = (int) changeType,
                DateChanged = DateTime.Now,
                HashedValue = _encryptionService.EncryptWithSalt(bankAccount.Bsb + "." + bankAccount.AccountNumber,
                    _settings.Value.SaltKey)
            };
            await context.MemberPaymentMethodHistory.AddAsync(paymentMethodHistory);
        }


        private async Task ValidateAccountExisting(int memberId, string accountNumber, string bsb, ShopGoContext context)
        {
            var existed = await context.MemberBankAccount
                .Where(account => account.MemberId != memberId &&
                                  account.AccountNumber == accountNumber &&
                                  account.Bsb == bsb)
                .Select(account => account.MemberId)
                .Distinct()
                .CountAsync();

            //one bank account can only link with two different CR accounts
            if (existed > 1)
                throw new DuplicateBankAccountException();
        }

        private static async Task<MemberBankAccount> ValidateAndGetMemberBankAccount(int memberId, ShopGoContext context)
        {
            var memberBankAccount = await context.MemberBankAccount.Where(acct =>
                    acct.MemberId == memberId && acct.Status == (int) StatusType.Active).OrderByDescending(acct => acct.DateCreated).AsNoTracking()
                .FirstOrDefaultAsync();
            if (memberBankAccount == null)
                throw new MemberBankAccountNotFoundException();
            return memberBankAccount;
        }

        private static async Task<Member> ValidateAndGetMemberStore(int memberId, ShopGoContext context)
        {
            var memberStore = await context.Member.Where(m =>
                    m.MemberId == memberId).AsNoTracking()
                .FirstOrDefaultAsync();

            if (memberStore == null)
                throw new MemberNotFoundException();

            return memberStore;
        }
    }
}