using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;
using static SettingsAPI.Common.Constant;

namespace SettingsAPI.Service
{
    public class TransactionService : ITransactionService
    {
        private readonly ShopGoContext _context;
        private readonly IValidationService _validationService;
        private readonly IMemberService _memberService;
        private readonly IOptions<Settings> _settings;

        public TransactionService(ShopGoContext context, IValidationService validationService, IMemberService memberService, IOptions<Settings> settings)
        {
            _context = context;
            _validationService = validationService;
            _memberService = memberService;
            _settings = settings;
        }

        public async Task<bool> HasApprovedPurchases(int memberId)
        {
            var hasRecords = await _context.Transaction.AnyAsync(tran =>
                tran.MemberId == memberId && tran.TransactionStatusId == TransactionStatus.Confirmed.GetHashCode() &&
                (tran.TransactionTypeId == 5 ||
                 tran.TransactionTypeId == 6));

            return hasRecords;
        }

        /// <summary>
        /// A flattened Superfast TransactionView which inserts the required number of {0} params for the supplied memberIds that will be passed to it.
        /// </summary>
        /// <param name="memberIds">Member ids that will be passed to this sql statement so the params can be generated.</param>
        /// <returns>SQL with the correct number of params inserted for the number of memberIds</returns>
        private string GetTransactionsFastSql(int[] memberIds)
        {
            var placeholders = string.Join(",", Enumerable.Range(0, memberIds.Length)
                .Select(i => "{" + i + "}"));
            var sql = $@"
          SELECT
              TRA.TransactionId,
              TRA.MemberId,
              TRA.TransactionDisplayId,
              TRA.SaleDate,
              TRS.TransactionStatus,
              MER.MerchantName,
              TRV.SaleValue,
              CUR.CurrencyCode,
              (TRV.MemberCommissionValueAUD + ISNULL(TCOV.CashbackOffsetAUD,0)) AS MemberCommissionValueAUD,
              COALESCE(CM.ApprovalWaitDays, MER.ApprovalWaitDays) AS ApprovalWaitDays,
              CASE WHEN TRA.TransactionStatusId = 101 THEN 1 ELSE 0 END AS IsApproved,
              CASE WHEN TRA.TransactionStatusId = 100 THEN 1 ELSE 0 END AS IsPending,
              CASE WHEN TRA.TransactionStatusId = 102 THEN 1 ELSE 0 END AS IsDeclined,
              0 AS IsPaid,
              0 AS IsPaymentPending,
              TRA.MerchantId AS MerchantId,
              MER.IsConsumption AS IsConsumption,
                CAST(CONVERT(VARCHAR, TRA.DateApproved, 106) AS NVARCHAR(30)) AS DateApproved,
              0 AS TransactionType,
              TRA.NetworkId AS NetworkId,
              TRA.OrderId,
              TRA.TransactionTypeId,
              CAST(TRA.TransactionReference AS NVARCHAR(500)) AS TransactionReference,
              CAST(TQ.TQLinkedStatus AS NVARCHAR(50)) AS TQLinkedStatus,
              CAST(MS.MaxStatus AS NVARCHAR(50)) AS MaxStatus,
                TRA.SaleDateAest

          FROM [Transaction] TRA

          INNER JOIN [TransactionTierView] TRV ON TRA.TransactionId = TRV.TransactionId
          INNER JOIN [Member] MEM ON TRA.MemberId = MEM.MemberId
          INNER JOIN [Currency] CUR ON TRA.TransCurrencyId = CUR.CurrencyId
          INNER JOIN [TransactionStatus] TRS ON TRA.TransactionStatusId = trs.TransactionStatusId
          INNER JOIN [Merchant] MER ON tra.MerchantId = MER.MerchantId
          LEFT OUTER JOIN [TransactionCashbackOffsetView] TCOV ON TRA.TransactionId = TCOV.TransactionId
          LEFT OUTER JOIN [TQLinked] AS TQ ON TRA.TQLinkedStatusId = TQ.TQLinkedId
          LEFT OUTER JOIN [ClientMerchant] AS CM ON CM.MerchantId = TRA.MerchantId AND CM.ClientId = TRA.ClientId
          LEFT OUTER JOIN [MaxStatus] AS MS ON TRA.MaxStatusId = MS.MaxStatusId

          WHERE TRA.[Status] = 1
                    AND MEM.[MemberId] IN ({placeholders})

UNION


          SELECT
              SV.SavingsId AS TransactionId,
              SV.MemberId,
              CAST(SV.SavingsId AS NVARCHAR(50)) AS TransactionDisplayId,
              SV.ActionDateAEST AS SaleDate,
              CAST('Approved' AS NVARCHAR(50)) AS TransactionStatus,
              ISNULL(SV.Description + ' Gift Cards', MER.MerchantName) AS MerchantName,
              SV.ActionValue AS SaleValue,
              CUR.CurrencyCode,
              SV.ActionSavingsValue AS MemberCommissionValueAUD,
              MER.ApprovalWaitDays  AS ApprovalWaitDays,
              1 AS IsApproved,
              0 AS IsPending,
              0 AS IsDeclined,
              0 AS IsPaid,
              0 AS IsPaymentPending,
              SV.MerchantId AS MerchantId,
              MER.IsConsumption AS IsConsumption,
              CAST(CONVERT(VARCHAR, SV.ActionDateAEST, 106) AS NVARCHAR(30)) AS DateApproved,
              1 AS TransactionType,
              MER.NetworkId AS NetworkId,
              CAST('' AS NVARCHAR(100)) AS OrderId,
              NULL AS TransactionTypeId,
              CAST(NULL AS NVARCHAR(500)) AS TransactionReference,
              CAST(NULL AS NVARCHAR(50)) AS TQLinkedStatus,
              CAST(NULL AS NVARCHAR(50)) AS MaxStatus,
                SV.ActionDateAEST as SaleDateAest

          FROM [Savings] SV

          INNER JOIN [Member] MEM ON SV.MemberId = MEM.MemberId
          INNER JOIN [Currency] CUR ON SV.CurrencyId = CUR.CurrencyId
          INNER JOIN [TransactionStatus] TRS ON SV.TransactionStatusId = trs.TransactionStatusId
          INNER JOIN [Merchant] MER ON SV.MerchantId = MER.MerchantId

          WHERE SV.[Status] = 1 AND MEM.[MemberId] IN ({placeholders})

                    UNION
          SELECT
              MR.RedeemId,
              MR.MemberId,
              CAST(MR.RedeemId AS NVARCHAR(50)) AS TransactionDisplayId,
              MR.DateRequested AS SaleDate, PS.PaymentStatus AS TransactionStatus,
              CASE WHEN MR.PaymentStatusId = 102 THEN 'Bank Payment Reversal' ELSE 'Payment' END AS MerchantName,
              MR.AmountRequested AS SaleValue,
              'AUD' AS CurrencyCode,
              MR.AmountRequested AS MemberCommissionValueAUD,
              0 AS ApprovalWaitDays,
              0 AS IsApproved,
              0 AS IsPending,
              --0 AS IsDeclined,
              CASE WHEN MR.PaymentStatusId = 102 THEN 1 ELSE 0 END AS IsDeclined ,
              CASE WHEN MR.PaymentStatusId = 100 THEN 1 ELSE 0 END AS IsPaid,
              CASE WHEN MR.PaymentStatusId = 101 THEN 1 ELSE 0 END AS IsPaymentPending,
              0 AS MerchantId,
              0 AS IsConsumption,
              CAST(NULL AS NVARCHAR(30)) AS DateApproved,
              2 AS TransactionType,
              1000018 AS NetworkId ,
              CAST(IIF(MR.IsPartial=1, MR.WithdrawalId+'-PARTIAL',WithdrawalId) AS NVARCHAR(100)) AS OrderId,
              NULL AS TransactionTypeId,
              CAST(MR.Comment AS NVARCHAR(500)) AS TransactionReference,
              CAST(NULL AS NVARCHAR(50)) AS TQLinkedStatus,
              CAST(NULL AS NVARCHAR(50)) AS MaxStatus,
                MR.DateRequested as SaleDateAest

          FROM [MemberRedeem] MR

          INNER JOIN [PaymentStatus] PS ON MR.PaymentStatusId = PS.PaymentStatusId
          WHERE MR.[MemberId] IN ({placeholders}) AND (CAST(IIF(MR.IsPartial=1, MR.WithdrawalId+'-PARTIAL',WithdrawalId) AS NVARCHAR(100)) IS NULL)
UNION
          SELECT
              MIN(MR.RedeemId) AS TransactionId,
              MIN(MR.MemberId) AS MemberId,
              CAST(MIN(MR.RedeemId) AS NVARCHAR(50)) AS TransactionDisplayId,
              MIN(MR.DateRequested) AS SaleDate,
                MIN(PS.PaymentStatus) AS TransactionStatus,
              CASE WHEN MIN(MR.PaymentStatusId) = 102 THEN 'Bank Payment Reversal' ELSE 'Payment' END AS MerchantName,
              SUM(MR.AmountRequested) AS SaleValue,
              'AUD' AS CurrencyCode,
              SUM(MR.AmountRequested) AS MemberCommissionValueAUD,
              0 AS ApprovalWaitDays,
              0 AS IsApproved,
              0 AS IsPending,
              --0 AS IsDeclined,
              CASE WHEN MIN(MR.PaymentStatusId) = 102 THEN 1 ELSE 0 END AS IsDeclined ,
              CASE WHEN MIN(MR.PaymentStatusId) = 100 THEN 1 ELSE 0 END AS IsPaid,
              CASE WHEN MIN(MR.PaymentStatusId) = 101 THEN 1 ELSE 0 END AS IsPaymentPending,
              0 AS MerchantId,
              0 AS IsConsumption,
              CAST(NULL AS NVARCHAR(30)) AS DateApproved,
              2 AS TransactionType,
              1000018 AS NetworkId ,
              CAST(IIF(MIN(CONVERT(int, MR.IsPartial))=1, MIN(MR.WithdrawalId)+'-PARTIAL',MIN(WithdrawalId)) AS NVARCHAR(100)) AS OrderId,
              NULL AS TransactionTypeId,
              CAST(NULL AS NVARCHAR(100)) AS TransactionReference,
              CAST(NULL AS NVARCHAR(50)) AS TQLinkedStatus,
              CAST(NULL AS NVARCHAR(50)) AS MaxStatus,
                MIN(MR.DateRequested) as SaleDateAest

          FROM [MemberRedeem] MR
          INNER JOIN [PaymentStatus] PS ON MR.PaymentStatusId = PS.PaymentStatusId

          WHERE MR.WithdrawalId IS NOT NULL AND MR.[MemberId] IN ({placeholders})
          GROUP BY MR.WithdrawalId

";
            return sql;
        }

        private IQueryable<TransactionView> TransactionViewsStoreRaw(int[] memberIds)
        {
            return _context.TransactionView
                .FromSqlRaw<TransactionView>(GetTransactionsFastSql(memberIds), memberIds.Cast<object>().ToArray()).AsQueryable<TransactionView>();
        }

        private IQueryable<TransactionView> TransactionViewsStoreEf(int[] memberIds)
        {
            return _context.TransactionView
            .Where(transactionView =>  memberIds.Contains(transactionView.MemberId));

        }

        private IQueryable<TransactionView> GetTransactionViewStore(int[] memberIds)
        {
            if (_settings.Value.TransactionMemberViewUseDatabaseView == true)
            {
                return this.TransactionViewsStoreEf(memberIds);
            }
            else 
            {
                return this.TransactionViewsStoreRaw(memberIds);
                
            }
        }

        public async Task<Paging<TransactionResult>> GetTransactions(int memberId, int limit, int offset,
            string searchText, string dateFromStr, string dateToStr, string orderBy, string sortDirection)
        {
            //Validate
            _validationService.ValidateQueryConditions(limit, offset, dateFromStr, dateToStr, orderBy, sortDirection, ApiUsed.Transaction);

            MembershipDetail membership = await _memberService.GetMembershipInfo(memberId);
            int[] memberIds = membership?.Items.Select(p => p.MemberId).ToArray();
            DateTime? premiumMemberDateJoined = membership.PremiumDateJoined;

            var transactionViewsStore = this.GetTransactionViewStore(memberIds);

            DateTime? dateFrom = null;
            if (dateFromStr != null)
                dateFrom = DateTime.ParseExact(dateFromStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            DateTime? dateTo = null;
            if (dateToStr != null)
                dateTo = DateTime.ParseExact(dateToStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            var transactionViewsQueryAble = transactionViewsStore
                .GroupJoin(
                    _context.Merchant,
                    tran => tran.MerchantId,
                    mer => mer.MerchantId,
                    (tran, mer) => new { tran, mer }
                )
                .SelectMany(
                    x => x.mer.DefaultIfEmpty(),
                    (result, mer) => new { result, mer });

            if (membership.IsPremium)
                transactionViewsQueryAble = transactionViewsQueryAble
                    .Where(row => !(row.mer.IsPremiumDisabled.HasValue && row.mer.IsPremiumDisabled.Value) ||
                                  !(premiumMemberDateJoined.HasValue && row.result.tran.SaleDate >= premiumMemberDateJoined));

            var queryAble = transactionViewsQueryAble
                .Select(row => 
                    new TransactionResult()
                    {
                        TransactionId = row.result.tran.TransactionId,
                        SaleDate = row.result.tran.SaleDate,
                        TransactionName = row.result.tran.TransactionType == 2 ? "Withdrawal" : row.result.tran.MerchantName,
                        TransactionType = ((TransactionType)row.result.tran.TransactionType).ToString(),
                        Currency = row.result.tran.CurrencyCode,
                        Status = row.result.tran.TransactionStatus,
                        IsConsumption = row.result.tran.IsConsumption == 1,
                        RegularImageUrl = row.result.tran.TransactionType == 2 ? "//www.cashrewards.com.au/assets/img/layout/cr-logo-icon.png" : row.mer.RegularImageUrl,
                        Amount = CalculateAmount(row.result.tran),
                        Commission = CalculateCommission(row.result.tran),
                        EstimatedApprovalDate = GetEstimatedApprovalDate(row.result.tran),
                        NetworkId = row.result.tran.NetworkId,
                        IsOverdue = TransactionIsOverdue(row.result.tran),
                        CashbackFlag = GetCashbackFlag(row.result.tran).ToString(),
                        MaxStatus = GetMaxStatus(row.result.tran, membership.PremiumStatus),
                        SaleDateAest=row.result.tran.SaleDateAest
                    }
                );

            if (dateFrom != null)
                queryAble = queryAble.Where(transaction => transaction.SaleDateAest >= dateFrom);

            if (dateTo != null)
                queryAble = queryAble.Where(transaction => transaction.SaleDateAest < dateTo.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(searchText))
                queryAble = queryAble.Where(transaction => transaction.TransactionName.Contains(searchText));

            //Calculate total count
            var totalCount = await queryAble.AsNoTracking().CountAsync();
            if (totalCount == 0)
                return new Paging<TransactionResult>(new List<TransactionResult>());

            var direction = SortDirection.Asc;
            if (sortDirection.Equals(SortDirection.Desc.ToString()))
                direction = SortDirection.Desc;

            List<TransactionResult> transactions = null;
            if (direction == SortDirection.Asc)
            {
                if (orderBy.Equals(TransactionOrderByField.Date.ToString()))
                    transactions = await queryAble.OrderBy(tran => tran.SaleDateAest).Skip(offset)
                        .Take(limit).AsNoTracking().ToListAsync();

                else if (orderBy.Equals(TransactionOrderByField.Name.ToString()))
                    transactions = await queryAble.OrderBy(tran => tran.TransactionName)
                        .Skip(offset).Take(limit).AsNoTracking().ToListAsync();

                else if (orderBy.Equals(TransactionOrderByField.Amount.ToString()))
                    transactions = queryAble.AsEnumerable().OrderBy(tran => tran.Amount)
                        .Skip(offset).Take(limit).ToList();

                else if (orderBy.Equals(TransactionOrderByField.Status.ToString()))
                    transactions = await queryAble.OrderBy(tran => tran.Status)
                        .Skip(offset).Take(limit).AsNoTracking().ToListAsync();
            }
            else
            {
                if (orderBy.Equals(TransactionOrderByField.Date.ToString()))
                    transactions = await queryAble.OrderByDescending(tran => tran.SaleDateAest)
                        .Skip(offset).Take(limit).AsNoTracking().ToListAsync();

                else if (orderBy.Equals(TransactionOrderByField.Name.ToString()))
                    transactions = await queryAble.OrderByDescending(tran => tran.TransactionName)
                        .Skip(offset).Take(limit).AsNoTracking().ToListAsync();

                else if (orderBy.Equals(TransactionOrderByField.Amount.ToString()))
                    transactions = queryAble.AsEnumerable().OrderByDescending(tran => tran.Amount).Skip(offset)
                        .Take(limit).ToList();

                else if (orderBy.Equals(TransactionOrderByField.Status.ToString()))
                    transactions = await queryAble.OrderByDescending(tran => tran.Status)
                        .Skip(offset).Take(limit).AsNoTracking().ToListAsync();
            }

            return new Paging<TransactionResult>(transactions);
        }

        public async Task<TotalCountResponse> GetTransactionsTotalCount(int memberId, string searchText,
            string dateFromStr, string dateToStr)
        {
            //Validate
            _validationService.ValidateQueryConditionsForTotalCount(dateFromStr, dateToStr);

            MembershipDetail membership = await _memberService.GetMembershipInfo(memberId);
            DateTime? premiumMemberDateJoined = membership.PremiumDateJoined;

            var transactionViewsStore = this.GetTransactionViewStore(new int[] { memberId });

            DateTime? dateFrom = null;
            if (dateFromStr != null)
                dateFrom = DateTime.ParseExact(dateFromStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            DateTime? dateTo = null;
            if (dateToStr != null)
                dateTo = DateTime.ParseExact(dateToStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            var transactionViewsQueryAble = transactionViewsStore
                .GroupJoin(
                    _context.Merchant,
                    tran => tran.MerchantId,
                    mer => mer.MerchantId,
                    (tran, mer) => new { tran, mer }
                )
                .SelectMany(
                    x => x.mer.DefaultIfEmpty(),
                    (result, mer) => new { result, mer });

            if (membership.IsPremium)
                transactionViewsQueryAble = transactionViewsQueryAble
                    .Where(row => !(row.mer.IsPremiumDisabled.HasValue && row.mer.IsPremiumDisabled.Value) ||
                                  !(premiumMemberDateJoined.HasValue && row.result.tran.SaleDate >= premiumMemberDateJoined));

            var queryAble = transactionViewsQueryAble
                .Select(row =>
                    new TransactionResult()
                    {
                        TransactionId = row.result.tran.TransactionId,
                        SaleDate = row.result.tran.SaleDate,
                        TransactionName = row.result.tran.TransactionType == 2 ? "Withdrawal" : row.result.tran.MerchantName,
                        TransactionType = row.result.tran.TransactionType == 2 ? "Cashback" : ((TransactionType)row.result.tran.TransactionType).ToString(),
                        Currency = row.result.tran.CurrencyCode,
                        Status = row.result.tran.TransactionStatus,
                        IsConsumption = row.result.tran.IsConsumption == 1,
                        RegularImageUrl = row.result.tran.TransactionType == 2 ? "//www.cashrewards.com.au/assets/img/layout/cr-logo-icon.png" : row.mer.RegularImageUrl,
                        Amount = CalculateAmount(row.result.tran),
                        Commission = CalculateCommission(row.result.tran),
                        EstimatedApprovalDate = GetEstimatedApprovalDate(row.result.tran),
                        NetworkId = row.result.tran.NetworkId
                    }
                );

            if (dateFrom != null)
                queryAble = queryAble.Where(transaction => transaction.SaleDate >= dateFrom);

            if (dateTo != null)
                queryAble = queryAble.Where(transaction => transaction.SaleDate < dateTo.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(searchText))
                queryAble = queryAble.Where(transaction => transaction.TransactionName.Contains(searchText));

            //Calculate total count
            var totalCount = await queryAble.AsNoTracking().CountAsync();
            return new TotalCountResponse
            {
                TotalCount = totalCount
            };
        }

        private static bool TransactionIsOverdue(TransactionView transactionView)
        {
            if (transactionView.TransactionStatus == "Pending" &&
                transactionView.SaleDate.AddDays(transactionView.ApprovalWaitDays) < DateTime.Now &&
                transactionView.MerchantId != Constant.CashRewardsReferAMateMerchantId)
            {
                return true;
            }

            return false;

        }

        private static decimal CalculateAmount(TransactionView transaction)
        {
            if (transaction.SaleValue == null) return 0.00M;

            if (transaction.TransactionType == 2) return Math.Round(transaction.SaleValue.Value * -1, 2);

            return Math.Abs(Math.Round(transaction.SaleValue.Value, 2));
        }

        private static decimal CalculateCommission(TransactionView transaction)
        {
            if (transaction.MemberCommissionValueAUD == null || transaction.TransactionType == 2) return 0.00M;

            if (transaction.IsPaid == 1 || transaction.IsPaymentPending == 1 || transaction.IsDeclined == 1)
                transaction.MemberCommissionValueAUD = 0;

            return Math.Round(transaction.MemberCommissionValueAUD.Value, 2);
        }

        private static string GetEstimatedApprovalDate(TransactionView transaction)
        {
            var approvalDate = "";
            var giftCardShows = new List<int> {1001330, 1001846, 1001847};

            var datealert = transaction.SaleDate.AddDays(transaction.ApprovalWaitDays);
            var dateAlertString = datealert.ToString(Constant.DatePrintFormat);

            if (transaction.TransactionStatus == TransactionStatus.Pending.ToString() && transaction.IsConsumption == 1)
                approvalDate = transaction.ApprovalWaitDays + " days from travel completion";

            else if (transaction.TransactionStatus == TransactionStatus.Pending.ToString() &&
                     transaction.MerchantId == Constant.CashRewardsReferAMateMerchantId)
            {
                if (transaction.TransactionTypeId == 1)
                    approvalDate = AppMessage.ApprovedOnceYourQualifyingConfirmed;

                else if (transaction.TransactionTypeId == 2)
                    approvalDate = AppMessage.ApprovedOnceYourFriendQualifyingConfirmed;
            }
            else if (transaction.TransactionStatus == TransactionStatus.Pending.ToString() &&
                     transaction.MerchantId == Constant.CashRewardsBonusMerchantId)
                approvalDate = AppMessage.ApprovedOnceYourQualifyingConfirmed;

            else if (giftCardShows.Contains(transaction.MerchantId))
                approvalDate = "-";

            else if (transaction.TransactionStatus == TransactionStatus.Pending.ToString() &&
                     Convert.ToDateTime(datealert).Date > DateTime.Now.Date)
                approvalDate = dateAlertString;

            else if (transaction.TransactionStatus == TransactionStatus.Pending.ToString() &&
                     Convert.ToDateTime(datealert).Date <= DateTime.Now.Date)
                approvalDate = dateAlertString;

            else if (transaction.TransactionStatus == TransactionStatus.Approved.ToString() &&
                     !string.IsNullOrEmpty(transaction.DateApproved))
                approvalDate = Convert.ToDateTime(transaction.DateApproved).ToString(Constant.DatePrintFormat);

            else if (transaction.TransactionStatus == TransactionStatus.Approved.ToString() &&
                     string.IsNullOrEmpty(transaction.DateApproved))
                approvalDate = "-";

            else if (transaction.TransactionStatus == TransactionStatus.Declined.ToString() && transaction.TransactionType == TransactionType.Cashback.GetHashCode())
                approvalDate = "Cashback was not approved";

            else if (transaction.TransactionStatus == TransactionStatus.Declined.ToString() &&
                     !string.IsNullOrEmpty(transaction.DateApproved))
                approvalDate = Convert.ToDateTime(transaction.DateApproved).ToString(Constant.DatePrintFormat);

            else if (transaction.TransactionStatus == TransactionStatus.Declined.ToString() &&
                     string.IsNullOrEmpty(transaction.DateApproved))
                approvalDate = "-";

            else if (string.IsNullOrEmpty(transaction.DateApproved))
                approvalDate = "-";

            else
                approvalDate = Convert.ToDateTime(transaction.DateApproved).ToString(Constant.DatePrintFormat);

            return approvalDate;
        }

        private static CashbackFlag GetCashbackFlag(TransactionView transactionView)
        {
            if(transactionView.TransactionType != (int)TransactionType.Cashback)
            {
                return CashbackFlag.None;
            }

            if(transactionView.MerchantId == Constant.CashRewardsReferAMateMerchantId)
            {
                if (transactionView.TransactionTypeId == (int)CashbackTransactionType.RafFriend)
                {
                    return CashbackFlag.RafFriend;
                }
                else
                {
                    return CashbackFlag.RafReferrer;
                }
            }

            if(transactionView.MerchantId == Constant.CashRewardsBonusMerchantId || transactionView.MerchantId == Constant.CashRewardsWelcomeBonusMerchantId || transactionView.MerchantId == Constant.CashRewardsPromotionalBonusMerchantId)
            {
                return CashbackFlag.Bonus;
            }

            if (transactionView.NetworkId == Networks.InStoreNetwork)
            {
                return CashbackFlag.CashbackInStore;
            }
            else
            {
                return CashbackFlag.CashbackOnline;
            }
        }
        private static string GetMaxStatus(TransactionView tran, int premiumStatus)
        {
            if(tran.MaxStatus == nameof(MaxStatusEnum.Pending) && (DateTime.Now.Date >= tran.SaleDate.AddDays(16) || premiumStatus == (int)PremiumStatusEnum.OptOut))
            {
                return nameof(MaxStatusEnum.NotApplicable);
            }

            return tran.MaxStatus;
        }
    }
}