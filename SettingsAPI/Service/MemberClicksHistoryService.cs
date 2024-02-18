using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;
using static SettingsAPI.Common.Constant;

namespace SettingsAPI.Service
{
    public class MemberClicksHistoryService : IMemberClicksHistoryService
    {
        private readonly ShopGoContext _context;
        private readonly ReadOnlyShopGoContext _readOnlyContext;
        private readonly IValidationService _validationService;
        private readonly IMemberService _memberService;

        public MemberClicksHistoryService(ShopGoContext context, ReadOnlyShopGoContext readOnlyContext, IValidationService validationService, IMemberService memberService)
        {
            _context = context;
            _readOnlyContext = readOnlyContext;
            _validationService = validationService;
            _memberService = memberService;
        }

        public async Task<Paging<MemberClicksHistoryResult>> GetMemberClicksHistory(int memberId, int limit, int offset,
            string searchText, string dateFromStr, string dateToStr, string orderBy, string sortDirection)
        {
            //Validate
            _validationService.ValidateQueryConditions(limit, offset, dateFromStr, dateToStr, orderBy, sortDirection, ApiUsed.MemberClickHistory);

            MembershipDetail membership = await _memberService.GetMembershipInfo(memberId);
            DateTime? premiumMemberDateJoined = membership.PremiumDateJoined;

            var clickHistory = _readOnlyContext.MemberClicks
                .Where(click => click.MemberId == memberId);

            DateTime? dateFrom = null;
            if (dateFromStr != null)
                dateFrom = DateTime.ParseExact(dateFromStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            DateTime? dateTo = null;
            if (dateToStr != null)
                dateTo = DateTime.ParseExact(dateToStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            var clickHistoryQueryAble = clickHistory
                .Join(_readOnlyContext.Merchant, click => click.MerchantId, mer => mer.MerchantId, (click, mer) => new { click, mer });

            if (membership.IsPremium)
                clickHistoryQueryAble = clickHistoryQueryAble
                    .Where(row => !(row.mer.IsPremiumDisabled.HasValue && row.mer.IsPremiumDisabled.Value) ||
                                    !(premiumMemberDateJoined.HasValue && row.click.DateCreated >= premiumMemberDateJoined));

            var queryAble = clickHistoryQueryAble.Select(row =>
                    new MemberClicksHistoryResult()
                    { 
                        ClickId = row.click.ClickId,
                        Date = row.click.DateCreated,
                        Store = row.mer.MerchantName,
                        RegularImageUrl = row.mer.RegularImageUrl
                    });

            if (dateFrom != null)
                queryAble = queryAble.Where(click =>
                    click.Date >= dateFrom);

            if (dateTo != null)
                queryAble = queryAble.Where(click =>
                    click.Date < dateTo.Value.AddDays(1)); // <=> date <= [date to] :23:59:59

            if (!string.IsNullOrWhiteSpace(searchText))
                queryAble = queryAble.Where(click => click.Store.Contains(searchText));

            //Calculate total count
            var totalCount = await queryAble.AsNoTracking().CountAsync();
            if (totalCount == 0)
                return new Paging<MemberClicksHistoryResult>(new List<MemberClicksHistoryResult>());

            var direction = SortDirection.Asc;
            if (sortDirection.Equals(SortDirection.Desc.ToString()))
                direction = SortDirection.Desc;

            List<MemberClicksHistoryResult> memberClicksHistoryResults = null;
            if (direction == SortDirection.Asc)
            {
                if (orderBy.Equals(TransactionOrderByField.Date.ToString()))
                    memberClicksHistoryResults =
                        await queryAble.OrderBy(click => click.Date)
                            .Skip(offset).Take(limit).AsNoTracking().ToListAsync();
            }
            else
            {
                if (orderBy.Equals(TransactionOrderByField.Date.ToString()))
                    memberClicksHistoryResults = await queryAble.OrderByDescending(click => click.Date)
                        .Skip(offset).Take(limit).AsNoTracking().ToListAsync();
            }

            return new Paging<MemberClicksHistoryResult>(memberClicksHistoryResults);
        }

        public async Task<TotalCountResponse> GetMemberClicksHistoryTotalCount(int memberId, string searchText,
            string dateFromStr, string dateToStr)
        {
            //Validate
            _validationService.ValidateQueryConditionsForTotalCount(dateFromStr, dateToStr);

            MembershipDetail membership = await _memberService.GetMembershipInfo(memberId);
            DateTime? premiumMemberDateJoined = membership.PremiumDateJoined;

            var clickHistory = _readOnlyContext.MemberClicks
                .Where(click => click.MemberId == memberId);

            DateTime? dateFrom = null;
            if (dateFromStr != null)
                dateFrom = DateTime.ParseExact(dateFromStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            DateTime? dateTo = null;
            if (dateToStr != null)
                dateTo = DateTime.ParseExact(dateToStr, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);

            var clickHistoryQueryAble = clickHistory
                .Join(_readOnlyContext.Merchant, click => click.MerchantId, mer => mer.MerchantId, (click, mer) => new { click, mer });

            if (membership.IsPremium)
                clickHistoryQueryAble = clickHistoryQueryAble
                        .Where(row => !(row.mer.IsPremiumDisabled.HasValue && row.mer.IsPremiumDisabled.Value) ||
                                      !(premiumMemberDateJoined.HasValue && row.click.DateCreated >= premiumMemberDateJoined));

            var queryAble = clickHistoryQueryAble
                .Select(row =>
                    new MemberClicksHistoryResult()
                    {
                        ClickId = row.click.ClickId,
                        Date = row.click.DateCreated,
                        Store = row.mer.MerchantName,
                        RegularImageUrl = row.mer.RegularImageUrl
                    });

            if (dateFrom != null)
                queryAble = queryAble.Where(click =>
                    click.Date >= dateFrom);

            if (dateTo != null)
                queryAble = queryAble.Where(click =>
                    click.Date < dateTo.Value.AddDays(1)); //<=> date <= [date to] :23:59:59

            if (!string.IsNullOrWhiteSpace(searchText))
                queryAble = queryAble.Where(click => click.Store.Contains(searchText));

            //Calculate total
            var totalCount = await queryAble.AsNoTracking().CountAsync();

            return new TotalCountResponse
            {
                TotalCount = totalCount
            };
        }
    }
}