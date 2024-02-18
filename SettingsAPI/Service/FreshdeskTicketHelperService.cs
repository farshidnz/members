using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using SettingsAPI.Common;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest.CreateTicket;

namespace SettingsAPI.Service
{
    public class FreshdeskTicketHelperService : IFreshdeskTicketHelperService
    {
        private readonly IRestClient _restClient;

        public FreshdeskTicketHelperService(IRestClient restClient)
        {
            _restClient = restClient;
        }
        public async Task<bool> CreateFreshDeskTicket(string freshdeskApiKey, string freshdeskDomain,
            int memberId, CreateTicketRequest ticketRequest, int premiumStatus)
        {
            var url = "https://" + freshdeskDomain + ".freshdesk.com" + "/api/v2/tickets";
            _restClient.BaseUrl = new Uri(url);
            _restClient.Authenticator = new HttpBasicAuthenticator(freshdeskApiKey, "X");
            
            var request = new RestRequest("", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddParameter("email", ticketRequest.Contact);
            request.AddParameter("status", TicketStatus.Open.GetHashCode());

            //Custom fields
            request.AddParameter("custom_fields[cf_first_name]", ticketRequest.FirstName);
            request.AddParameter("custom_fields[cf_last_name]", ticketRequest.LastName);

            var attachmentPaths = new List<string>();
            //Invoice
            if (ticketRequest.OrderConfirmationOrInvoices != null &&
                ticketRequest.OrderConfirmationOrInvoices.Count > 0)
            {
                foreach (var file in ticketRequest.OrderConfirmationOrInvoices)
                {
                    var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    var bytes = stream.ToArray();
                    request.AddFile("attachments[]", bytes, file.FileName, file.ContentType);
                    attachmentPaths.Add(file.FileName);
                }
            }
            
            var dateOfPurchaseFormat = string.Empty;
            if (!string.IsNullOrWhiteSpace(ticketRequest.DateOfPurchase))
            {
                var dateOfPurchase = DateTime.ParseExact(
                    ticketRequest.DateOfPurchase, Constant.DateQueryParameterFormat, CultureInfo.InvariantCulture
                );
                
                dateOfPurchaseFormat = dateOfPurchase.ToString(Constant.DatePrintFormat);
            }

            var saleValueFormat = ticketRequest.SaleValueTracked;
            if (!string.IsNullOrWhiteSpace(saleValueFormat))
                saleValueFormat = "$" + $"{decimal.Parse(ticketRequest.SaleValueTracked):0.00}";

            var cashbackFormat = ticketRequest.Cashback;
            if (!string.IsNullOrWhiteSpace(cashbackFormat))
                cashbackFormat = "$" + $"{decimal.Parse(ticketRequest.Cashback):0.00}";

            var saleValueExpectedFormat = ticketRequest.SaleValueExcepted;
            if (!string.IsNullOrWhiteSpace(saleValueExpectedFormat))
                saleValueExpectedFormat = "$" + $"{decimal.Parse(saleValueExpectedFormat):0.00}";

            var cashbackExpected = ticketRequest.CashbackOptional;

            var premiumStatusDescription = string.Empty;

            if (PremiumStatusEnum.Enrolled.GetHashCode() == premiumStatus)
                premiumStatusDescription = Util.GetDescriptionFromEnum(PremiumStatusTicketEnum.Enrolled);
            
            if (PremiumStatusEnum.NotEnrolled.GetHashCode() == premiumStatus)
                premiumStatusDescription = Util.GetDescriptionFromEnum(PremiumStatusTicketEnum.NotEnrolled);
            
            if (PremiumStatusEnum.OptOut.GetHashCode() == premiumStatus)
                premiumStatusDescription = Util.GetDescriptionFromEnum(PremiumStatusTicketEnum.OptOut);

            if (!string.IsNullOrWhiteSpace(premiumStatusDescription))
                request.AddParameter("custom_fields[cf_premium_status]", premiumStatusDescription);

            var enquireReason = ticketRequest.EnquiryReason;

            var enquireReasonEnum = Util.GetEnumFromDescription<EnquiryReason>(enquireReason);

            switch (enquireReasonEnum)
            {
                //Create ticket for Cashback Claim flow
                case EnquiryReason.CashbackClaim:
                {
                    var descriptionBuilder = new StringBuilder("<strong>Premium status</strong> : " + premiumStatusDescription + "<br>"
                                                               + "<strong>Date of Purchase</strong> : " + dateOfPurchaseFormat + "<br>"
                                                               + "<strong>Store</strong> : " + ticketRequest.Store + "<br>"
                                                               + "<strong>Currency</strong> : " + Currency.AUD + "<br>"
                                                               + "<strong>Sale value</strong> : " + saleValueExpectedFormat + "<br>"
                                                               + "<strong>Expected cashback rate (optional)</strong> : " +
                                                               cashbackExpected + "<br>"
                                                               +"<strong>Invoice or Order ID</strong> : " + ticketRequest.OrderId + "<br>"
                                                               + "<strong>Did you apply any additional discounts to your purchase (e.g coupon codes)</strong> : " +
                                                               ticketRequest.ApplyDiscounts + "<br>");

                    if (!string.IsNullOrWhiteSpace(ticketRequest.DiscountType))
                    {
                        var discountType = ticketRequest.DiscountType;
                        if (Util.GetDescriptionFromEnum(DiscountType.Other).Equals(discountType))
                            discountType = "Other";

                        descriptionBuilder.Append("<strong>Mode of discount</strong> : " + discountType +
                                                  "<br>");

                        if (DiscountType.Other == Util.GetEnumFromDescription<DiscountType>(ticketRequest.DiscountType))
                            descriptionBuilder.Append("<strong>Other</strong> : " +
                                                      ticketRequest.DiscountCode +
                                                      "<br>");
                        else
                            descriptionBuilder.Append("<strong>Coupon code used</strong> : " +
                                                      ticketRequest.DiscountCode +
                                                      "<br>");

                        request.AddParameter("custom_fields[cf_mode_of_discount]", ticketRequest.DiscountType);
                    }
                    descriptionBuilder.Append("<strong>Did you make your purchase via the Cashrewards App?</strong> : " +
                                               ticketRequest.PurchaseApp + "<br>"
                                               + "<strong>MemberId</strong> : " + memberId + "<br>"
                                               + "<strong>Member Statement</strong> : " + Constant.ShopGoBaseUrl +
                                               "/Transaction/ClientMemberTransaction.aspx?itemid=" + memberId + "<br>"
                                               + "<strong>Member Clicks</strong> : " + Constant.ShopGoBaseUrl +
                                               "/Client/ClientMemberClickList.aspx?itemid=" + memberId + "<br>" + 
                                               "<strong>Additional information</strong> : " + ticketRequest.Info + "<br>");

                    request.AddParameter("subject",
                        string.Format(Constant.TicketSubject, "Cashback Claim", ticketRequest.Store));
                    
                    request.AddParameter("description", descriptionBuilder.ToString());
                    
                    request.AddParameter("custom_fields[cf_currency]", Currency.AUD.ToString());

                    request.AddParameter("type", Util.GetDescriptionFromEnum(TicketType.UserIncident));
                    
                    request.AddParameter("custom_fields[cf_what_can_we_help_you_with]",
                        Util.GetDescriptionFromEnum(CashbackProblemHelp.CashbackClaim));
                    
                    request.AddParameter("custom_fields[cf_mode_of_purchase]", Util.GetDescriptionFromEnum(ModOfPurchase.Online));

                    var groupId = TicketGroup.CashbackClaims.GetHashCode();
                    request.AddParameter("group_id", groupId);

                    var priority = TicketPriority.Low.GetHashCode();
                    request.AddParameter("priority", priority);
                    
                    var premiumMemberEnum = Util.GetEnumFromDescription<PremiumMember>(ticketRequest.PremiumMember);
                    var productId = premiumMemberEnum == PremiumMember.Yes
                        ? TicketProduct.Max.GetHashCode()
                        : TicketProduct.Cashrewards.GetHashCode();
                    request.AddParameter("product_id", productId);
                    
                    request.AddParameter("custom_fields[cf_please_select_a_topic_of_your_enquiry]",
                        Util.GetDescriptionFromEnum(TopicEnquiry.CashbackIssues));
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.DateOfPurchase))
                        request.AddParameter("custom_fields[cf_date_of_purchase]", ticketRequest.DateOfPurchase);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.Store))
                        request.AddParameter("custom_fields[cf_store]", ticketRequest.Store);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.OrderId))
                        request.AddParameter("custom_fields[cf_invoice_or_order_id]", ticketRequest.OrderId);
                    
                    if (!string.IsNullOrWhiteSpace(saleValueFormat))
                        request.AddParameter("custom_fields[cf_sale_value]", saleValueFormat);
                    
                    if (!string.IsNullOrWhiteSpace(cashbackFormat))
                        request.AddParameter("custom_fields[cf_cashback]", cashbackFormat);
                    
                    if (!string.IsNullOrWhiteSpace(cashbackExpected))
                        request.AddParameter("custom_fields[cf_expected_cashback_rateoptional]", cashbackExpected);
                    
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.ApplyDiscounts))
                        request.AddParameter(
                            "custom_fields[cf_did_you_apply_additional_discounts_to_your_purchase]",
                            ticketRequest.ApplyDiscounts);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.DiscountCode) &&
                        DiscountType.CouponOrVoucher == Util.GetEnumFromDescription<DiscountType>(ticketRequest.DiscountType))
                    {
                        request.AddParameter("custom_fields[cf_mode_of_discount_coupon_code_appliedothers]",
                            ticketRequest.DiscountCode);

                    }
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.PurchaseApp))
                        request.AddParameter("custom_fields[cf_did_you_make_your_purchase_via_the_cashrewards_app]",
                            ticketRequest.PurchaseApp);

                    if (!string.IsNullOrWhiteSpace(saleValueExpectedFormat))
                        request.AddParameter("custom_fields[cf_sale_value]", saleValueExpectedFormat);

                    break;
                }
                //Create ticket for Overdue Bonus + Declined Bonus flow
                case EnquiryReason.NoCashbackBonus:
                {
                    var descriptionFormat = "<strong>Premium status</strong> : " + premiumStatusDescription + "<br>"
                                      + "<strong>{0}</strong> : " + dateOfPurchaseFormat + "<br>"
                                      + "<strong>Store</strong> : " + ticketRequest.Store + "<br>"
                                      + "<strong>Currency</strong> : " + Currency.AUD + "<br>"
                                      + "<strong>Sale value</strong> : " + saleValueFormat + "<br>"
                                      + "<strong>Bonus amount</strong> : " + cashbackFormat + "<br>"
                                      + "<strong>MemberId</strong> : " + memberId + "<br>"
                                      + "<strong>Member Statement</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Transaction/ClientMemberTransaction.aspx?itemid=" + memberId + "<br>"
                                      + "<strong>Member Clicks</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Client/ClientMemberClickList.aspx?itemid=" + memberId + "<br>" +
                                      "<strong>Member Transaction</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Transaction/transactionedit.aspx?itemid=" + ticketRequest.TransactionId + "<br>"
                                      + "<strong>Additional information</strong> : " + ticketRequest.Info + "<br>";

                    var bonusIssueEnum = Util.GetEnumFromDescription<BonusIssue>(ticketRequest.BonusIssue);

                    var subject = (bonusIssueEnum == BonusIssue.OverduePendingBonus)
                        ? string.Format(Constant.TicketSubject, "Overdue Pending Bonus", ticketRequest.Store)
                        : string.Format(Constant.TicketSubject, "Declined Bonus", ticketRequest.Store);
                    
                    request.AddParameter("subject", subject);

                    var description = (bonusIssueEnum == BonusIssue.OverduePendingBonus)
                        ? string.Format(descriptionFormat, "Date of Purchase")
                        : string.Format(descriptionFormat, "Bonus date");
                    
                    request.AddParameter("description", description);
                    
                    request.AddParameter("custom_fields[cf_currency]", Currency.AUD.ToString());
                    
                    request.AddParameter("custom_fields[cf_what_can_we_help_you_with]",
                        Util.GetDescriptionFromEnum(CashbackProblemHelp.MyRewardsSupport));

                    var groupId = bonusIssueEnum == BonusIssue.OverduePendingBonus
                        ? TicketGroup.Approvals.GetHashCode()
                        : TicketGroup.IncorrectlyDeclined.GetHashCode();
                    
                    request.AddParameter("group_id", groupId);

                    var priority = bonusIssueEnum == BonusIssue.OverduePendingBonus
                        ? TicketPriority.High.GetHashCode()
                        : TicketPriority.Medium.GetHashCode();
                    request.AddParameter("priority", priority);
                    
                    var premiumMemberEnum = Util.GetEnumFromDescription<PremiumMember>(ticketRequest.PremiumMember);
                    var productId = premiumMemberEnum == PremiumMember.Yes
                        ? TicketProduct.Max.GetHashCode()
                        : TicketProduct.Cashrewards.GetHashCode();

                    request.AddParameter("product_id", productId);
                    
                    request.AddParameter("custom_fields[cf_please_select_a_topic_of_your_enquiry]",
                        Util.GetDescriptionFromEnum(TopicEnquiry.CashbackIssues));
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.DateOfPurchase))
                        request.AddParameter("custom_fields[cf_date_of_purchase]", ticketRequest.DateOfPurchase);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.Store))
                        request.AddParameter("custom_fields[cf_store]", ticketRequest.Store);
                    
                    if (!string.IsNullOrWhiteSpace(saleValueFormat))
                        request.AddParameter("custom_fields[cf_sale_value]", saleValueFormat);
                    
                    if (!string.IsNullOrWhiteSpace(cashbackFormat))
                        request.AddParameter("custom_fields[cf_cashback]", cashbackFormat);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.EstimateApprovalTimeframe))
                        request.AddParameter("custom_fields[cf_estimate_approval_timeframe]",
                            ticketRequest.EstimateApprovalTimeframe);

                    var natureOfEnquiryEnum = bonusIssueEnum == BonusIssue.OverduePendingBonus
                        ? NatureOfEnquiry.OverduePendingRewards
                        : NatureOfEnquiry.CashbackRewardsDeclined;
                    request.AddParameter("custom_fields[cf_what_is_the_nature_of_your_enquiry]",
                        Util.GetDescriptionFromEnum(natureOfEnquiryEnum));
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.IsApprovalDatePass) && bonusIssueEnum == BonusIssue.OverduePendingBonus)
                        request.AddParameter("custom_fields[cf_has_your_estimate_approval_date_passed]",
                            ticketRequest.IsApprovalDatePass);
                    
                    if (!string.IsNullOrWhiteSpace(saleValueExpectedFormat) && bonusIssueEnum == BonusIssue.DeclinedBonus)
                        request.AddParameter("custom_fields[cf_purchase_sale_value]", saleValueExpectedFormat);
                    
                    if (!string.IsNullOrWhiteSpace(cashbackExpected) && bonusIssueEnum == BonusIssue.DeclinedBonus)
                        request.AddParameter("custom_fields[cf_purchase_cashback]", cashbackExpected);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.OrderId) && bonusIssueEnum == BonusIssue.DeclinedBonus)
                        request.AddParameter("custom_fields[cf_invoice_or_order_id]", ticketRequest.OrderId);

                    if (!string.IsNullOrWhiteSpace(ticketRequest.AdditionalInformation))
                        request.AddParameter("custom_fields[cf_how_can_we_help]", ticketRequest.AdditionalInformation);

                    if (!string.IsNullOrWhiteSpace(ticketRequest.Info))
                        request.AddParameter("custom_fields[cf_please_provide_any_other_additional_details]",
                            ticketRequest.Info);

                    break;
                }
                //Create ticket for Payment Support flow
                case EnquiryReason.OverdueWithdrawal:
                case EnquiryReason.Other:
                {
                    var description = "<strong>Premium status</strong> : " + premiumStatusDescription + "<br>"
                                      + "<strong>Date of Withdrawal</strong> : " + dateOfPurchaseFormat + "<br>"
                                      + "<strong>Store</strong> : " + ticketRequest.Store + "<br>"
                                      + "<strong>Currency</strong> : " + Currency.AUD + "<br>"
                                      + "<strong>Withdrawal amount</strong> : " + saleValueFormat + "<br>"
                                      + "<strong>Additional information</strong> : " +
                                      ticketRequest.Info + "<br>"
                                      + "<strong>How can we help with your withdrawal</strong> : " +
                                      ticketRequest.AdditionalInformation + "<br>"
                                      + "<strong>MemberId</strong> : " + memberId + "<br>"
                                      + "<strong>Member Statement</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Transaction/ClientMemberTransaction.aspx?itemid=" + memberId + "<br>"
                                      + "<strong>Member Clicks</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Client/ClientMemberClickList.aspx?itemid=" + memberId + "<br>";

                    var subject = enquireReasonEnum == EnquiryReason.OverdueWithdrawal
                        ? string.Format(Constant.TicketSubject, "Overdue Withdrawal", ticketRequest.Store)
                        : string.Format(Constant.TicketSubject, "Other", ticketRequest.Store);

                    request.AddParameter("subject", subject);
                    
                    request.AddParameter("description", description);
                    
                    request.AddParameter("custom_fields[cf_currency]", Currency.AUD.ToString());
                    
                    request.AddParameter("custom_fields[cf_what_can_we_help_you_with]",
                        Util.GetDescriptionFromEnum(CashbackProblemHelp.MyRewardsSupport));
                    
                    request.AddParameter("group_id", TicketGroup.PaymentSupport.GetHashCode());

                    request.AddParameter("priority", TicketPriority.High.GetHashCode());
                    
                    var premiumMemberEnum = Util.GetEnumFromDescription<PremiumMember>(ticketRequest.PremiumMember);
                    var productId = premiumMemberEnum == PremiumMember.Yes
                        ? TicketProduct.Max.GetHashCode()
                        : TicketProduct.Cashrewards.GetHashCode();

                    request.AddParameter("product_id", productId);
                    
                    request.AddParameter("custom_fields[cf_please_select_a_topic_of_your_enquiry]",
                        Util.GetDescriptionFromEnum(TopicEnquiry.PaymentIssues));
                    
                    request.AddParameter("custom_fields[cf_payment_issues]",
                        Util.GetDescriptionFromEnum(PaymentIssuesEnum.PaymentHasNotBeenPaid));
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.Info)) 
                        request.AddParameter("custom_fields[cf_please_provide_any_other_additional_details]", ticketRequest.Info);

                    break;
                }
                //Create ticket for Refer a Friend flows
                case EnquiryReason.OverdueReferralBonus:
                case EnquiryReason.DeclinedReferralBonus:
                {
                    var description = "<strong>Premium status</strong> : " + premiumStatusDescription + "<br>"
                                      + "<strong>Date of Purchase</strong> : " + dateOfPurchaseFormat + "<br>"
                                      + "<strong>Store</strong> : " + ticketRequest.Store + "<br>"
                                      + "<strong>Currency</strong> : " + Currency.AUD + "<br>"
                                      + "<strong>Sale value</strong> : " + saleValueFormat + "<br>"
                                      + "<strong>Bonus amount</strong> : " + cashbackFormat + "<br>"
                                      + "<strong>Additional information</strong> : " +
                                      ticketRequest.Info +
                                      "<br>"
                                      + "<strong>MemberId</strong> : " + memberId + "<br>"
                                      + "<strong>Member Statement</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Transaction/ClientMemberTransaction.aspx?itemid=" + memberId + "<br>"
                                      + "<strong>Member Clicks</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Client/ClientMemberClickList.aspx?itemid=" + memberId + "<br>" +
                                      "<strong>Member Transaction</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Transaction/transactionedit.aspx?itemid=" + ticketRequest.TransactionId + "<br>";

                    var subject = enquireReasonEnum == EnquiryReason.OverdueReferralBonus
                        ? string.Format(Constant.TicketSubject, "Overdue Referral Bonus", ticketRequest.Store)
                        : string.Format(Constant.TicketSubject, "Declined Referral Bonus", ticketRequest.Store);

                    request.AddParameter("subject", subject);
                    
                    request.AddParameter("description", description);
                    
                    request.AddParameter("custom_fields[cf_currency]", Currency.AUD.ToString());
                    
                     request.AddParameter("custom_fields[cf_what_can_we_help_you_with]",
                        Util.GetDescriptionFromEnum(CashbackProblemHelp.MyRewardsSupport));

                    var groupId = enquireReasonEnum == EnquiryReason.OverdueReferralBonus
                        ? TicketGroup.Approvals.GetHashCode()
                        : TicketGroup.IncorrectlyDeclined.GetHashCode();
                    
                    request.AddParameter("group_id", groupId);

                    var priority = enquireReasonEnum == EnquiryReason.OverdueReferralBonus
                        ? TicketPriority.High.GetHashCode()
                        : TicketPriority.Medium.GetHashCode();
                    request.AddParameter("priority", priority);
                    
                    var premiumMemberEnum = Util.GetEnumFromDescription<PremiumMember>(ticketRequest.PremiumMember);
                    var productId = premiumMemberEnum == PremiumMember.Yes
                        ? TicketProduct.Max.GetHashCode()
                        : TicketProduct.Cashrewards.GetHashCode();

                    request.AddParameter("product_id", productId);
                    
                    
                    request.AddParameter("custom_fields[cf_please_select_a_topic_of_your_enquiry]",
                        Util.GetDescriptionFromEnum(TopicEnquiry.CashbackIssues));
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.DateOfPurchase))
                        request.AddParameter("custom_fields[cf_date_of_purchase]", ticketRequest.DateOfPurchase);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.Store))
                        request.AddParameter("custom_fields[cf_store]", ticketRequest.Store);
                    
                    if (!string.IsNullOrWhiteSpace(saleValueFormat))
                        request.AddParameter("custom_fields[cf_sale_value]", saleValueFormat);
                    
                    if (!string.IsNullOrWhiteSpace(cashbackFormat))
                        request.AddParameter("custom_fields[cf_cashback]", cashbackFormat);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.EstimateApprovalTimeframe))
                        request.AddParameter("custom_fields[cf_estimate_approval_timeframe]",
                            ticketRequest.EstimateApprovalTimeframe);

                    var natureOfEnquiryEnum = enquireReasonEnum == EnquiryReason.OverdueReferralBonus
                        ? NatureOfEnquiry.OverduePendingRewards
                        : NatureOfEnquiry.CashbackRewardsDeclined;
                    request.AddParameter("custom_fields[cf_what_is_the_nature_of_your_enquiry]",
                        Util.GetDescriptionFromEnum(natureOfEnquiryEnum));
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.IsApprovalDatePass) && enquireReasonEnum == EnquiryReason.OverdueReferralBonus)
                        request.AddParameter("custom_fields[cf_has_your_estimate_approval_date_passed]",
                            ticketRequest.IsApprovalDatePass);
                    
                    if (!string.IsNullOrWhiteSpace(saleValueExpectedFormat) && enquireReasonEnum == EnquiryReason.DeclinedReferralBonus)
                        request.AddParameter("custom_fields[cf_purchase_sale_value]", saleValueExpectedFormat);
                    
                    if (!string.IsNullOrWhiteSpace(cashbackExpected) && enquireReasonEnum == EnquiryReason.DeclinedReferralBonus)
                        request.AddParameter("custom_fields[cf_purchase_cashback]", cashbackExpected);
                    
                    if (!string.IsNullOrWhiteSpace(ticketRequest.OrderId) && enquireReasonEnum == EnquiryReason.DeclinedReferralBonus)
                        request.AddParameter("custom_fields[cf_invoice_or_order_id]", ticketRequest.OrderId);

                    if (!string.IsNullOrWhiteSpace(ticketRequest.AdditionalInformation))
                        request.AddParameter("custom_fields[cf_how_can_we_help]", ticketRequest.AdditionalInformation);

                    if (!string.IsNullOrWhiteSpace(ticketRequest.Info))
                        request.AddParameter("custom_fields[cf_please_provide_any_other_additional_details]",
                            ticketRequest.Info);

                    break;
                }
                //Create ticket for Incorrect cashback + Overdue cashback + Declined cashback
                default:
                {
                    var description = "<strong>Premium status</strong> : " + premiumStatusDescription + "<br>"
                                      + "<strong>Date of Purchase</strong> : " + dateOfPurchaseFormat + "<br>"
                                      + "<strong>Store</strong> : " + ticketRequest.Store + "<br>"
                                      + "<strong>Sale value</strong> : " + saleValueFormat + "<br>"
                                      + "<strong>Cashback</strong> : " + cashbackFormat + "<br>"
                                      + "<strong>Estimate approval time frame</strong> : " +
                                      ticketRequest.EstimateApprovalTimeframe + "<br>"
                                      + "<strong>What is the nature of your enquiry</strong> : " +
                                      ticketRequest.EnquiryReason + "<br>"
                                      + "{0}"
                                      + "<strong>MemberId</strong> : " + memberId + "<br>"
                                      + "<strong>Member Statement</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Transaction/ClientMemberTransaction.aspx?itemid=" + memberId + "<br>"
                                      + "<strong>Member Clicks</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Client/ClientMemberClickList.aspx?itemid=" + memberId + "<br>"
                                      + "<strong>Member Transaction</strong> : " + Constant.ShopGoBaseUrl +
                                      "/Transaction/transactionedit.aspx?itemid=" + ticketRequest.TransactionId + "<br>"
                                      + "<strong>Additional information</strong> : " + ticketRequest.Info + "<br>";

                    var descriptionFormat = string.Empty;

                    int priority;
                    int groupId;
                    int productId;
                    PremiumMember premiumMemberEnum;
                    NatureOfEnquiry natureOfEnquiryEnum;
                    switch (enquireReasonEnum)
                    {
                        case EnquiryReason.IncorrectCashback:
                        {
                            descriptionFormat = "<strong>Expected Sale value</strong> : " +
                                                saleValueExpectedFormat +
                                                "<br>" + "<strong>Expected Cashback</strong> : " +
                                                cashbackExpected + "<br>" +
                                                "<strong>Invoice or Order ID</strong> : " +
                                                ticketRequest.OrderId +
                                                "<br>" + "<strong>Invoice</strong> : " +
                                                string.Join(", ", attachmentPaths) + "<br>";
                            

                            groupId = TicketGroup.IncorrectlyDeclined.GetHashCode();
                            
                            request.AddParameter("group_id", groupId);

                            priority = TicketPriority.Medium.GetHashCode();
                            request.AddParameter("priority", priority);
                            
                            premiumMemberEnum = Util.GetEnumFromDescription<PremiumMember>(ticketRequest.PremiumMember);
                            productId = premiumMemberEnum == PremiumMember.Yes
                                ? TicketProduct.Max.GetHashCode()
                                : TicketProduct.Cashrewards.GetHashCode();

                            request.AddParameter("product_id", productId);
                            
                            request.AddParameter("custom_fields[cf_please_select_a_topic_of_your_enquiry]",
                                Util.GetDescriptionFromEnum(TopicEnquiry.CashbackIssues));
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.DateOfPurchase))
                                request.AddParameter("custom_fields[cf_date_of_purchase]", ticketRequest.DateOfPurchase);
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.Store))
                                request.AddParameter("custom_fields[cf_store]", ticketRequest.Store);
                            
                            if (!string.IsNullOrWhiteSpace(saleValueFormat))
                                request.AddParameter("custom_fields[cf_sale_value]", saleValueFormat);
                            
                            if (!string.IsNullOrWhiteSpace(cashbackFormat))
                                request.AddParameter("custom_fields[cf_cashback]", cashbackFormat);
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.EstimateApprovalTimeframe))
                                request.AddParameter("custom_fields[cf_estimate_approval_timeframe]",
                                    ticketRequest.EstimateApprovalTimeframe);

                            natureOfEnquiryEnum = NatureOfEnquiry.IncorrectCashback;
                            request.AddParameter("custom_fields[cf_what_is_the_nature_of_your_enquiry]",
                                Util.GetDescriptionFromEnum(natureOfEnquiryEnum));
                            
                            if (!string.IsNullOrWhiteSpace(saleValueExpectedFormat))
                                request.AddParameter("custom_fields[cf_purchase_sale_value]", saleValueExpectedFormat);
                            
                            if (!string.IsNullOrWhiteSpace(cashbackExpected))
                                request.AddParameter("custom_fields[cf_purchase_cashback]", cashbackExpected);
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.OrderId))
                                request.AddParameter("custom_fields[cf_invoice_or_order_id]", ticketRequest.OrderId);
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.Info))
                                request.AddParameter("custom_fields[cf_please_provide_any_other_additional_details]", ticketRequest.Info);
                            
                            request.AddParameter("subject", 
                                string.Format(Constant.TicketSubject, "Incorrect Cashback", ticketRequest.Store));
                            
                            break;
                        }
                        case EnquiryReason.DeclinedCashback:
                        case EnquiryReason.OverdueCashback:
                            if (enquireReasonEnum == EnquiryReason.DeclinedCashback)
                                descriptionFormat = "<strong>Invoice or Order ID</strong> : " +
                                                    ticketRequest.OrderId +
                                                    "<br>" + "<strong>Invoice</strong> : " +
                                                    string.Join(", ", attachmentPaths) + "<br>";

                            groupId = enquireReasonEnum == EnquiryReason.OverdueCashback
                                ? TicketGroup.Approvals.GetHashCode()
                                : TicketGroup.IncorrectlyDeclined.GetHashCode();
                            
                            request.AddParameter("group_id", groupId);

                            priority = enquireReasonEnum == EnquiryReason.OverdueCashback
                                ? TicketPriority.High.GetHashCode()
                                : TicketPriority.Medium.GetHashCode();
                            request.AddParameter("priority", priority);
                            
                            premiumMemberEnum = Util.GetEnumFromDescription<PremiumMember>(ticketRequest.PremiumMember);
                            productId = premiumMemberEnum == PremiumMember.Yes
                                ? TicketProduct.Max.GetHashCode()
                                : TicketProduct.Cashrewards.GetHashCode();

                            request.AddParameter("product_id", productId);
                            
                            request.AddParameter("custom_fields[cf_please_select_a_topic_of_your_enquiry]",
                                Util.GetDescriptionFromEnum(TopicEnquiry.CashbackIssues));
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.DateOfPurchase))
                                request.AddParameter("custom_fields[cf_date_of_purchase]", ticketRequest.DateOfPurchase);
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.Store))
                                request.AddParameter("custom_fields[cf_store]", ticketRequest.Store);
                            
                            if (!string.IsNullOrWhiteSpace(saleValueFormat))
                                request.AddParameter("custom_fields[cf_sale_value]", saleValueFormat);
                            
                            if (!string.IsNullOrWhiteSpace(cashbackFormat))
                                request.AddParameter("custom_fields[cf_cashback]", cashbackFormat);
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.EstimateApprovalTimeframe))
                                request.AddParameter("custom_fields[cf_estimate_approval_timeframe]",
                                    ticketRequest.EstimateApprovalTimeframe);

                            natureOfEnquiryEnum = enquireReasonEnum == EnquiryReason.OverdueCashback
                                ? NatureOfEnquiry.OverduePendingRewards
                                : NatureOfEnquiry.CashbackRewardsDeclined;
                            request.AddParameter("custom_fields[cf_what_is_the_nature_of_your_enquiry]",
                                Util.GetDescriptionFromEnum(natureOfEnquiryEnum));
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.IsApprovalDatePass) && enquireReasonEnum == EnquiryReason.OverdueCashback)
                                request.AddParameter("custom_fields[cf_has_your_estimate_approval_date_passed]",
                                    ticketRequest.IsApprovalDatePass);
                            
                            if (!string.IsNullOrWhiteSpace(saleValueExpectedFormat) && enquireReasonEnum == EnquiryReason.DeclinedCashback)
                                request.AddParameter("custom_fields[cf_purchase_sale_value]", saleValueExpectedFormat);
                            
                            if (!string.IsNullOrWhiteSpace(cashbackExpected) && enquireReasonEnum == EnquiryReason.DeclinedCashback)
                                request.AddParameter("custom_fields[cf_purchase_cashback]", cashbackExpected);
                            
                            if (!string.IsNullOrWhiteSpace(ticketRequest.OrderId) && enquireReasonEnum == EnquiryReason.DeclinedCashback)
                                request.AddParameter("custom_fields[cf_invoice_or_order_id]", ticketRequest.OrderId);

                            if (!string.IsNullOrWhiteSpace(ticketRequest.AdditionalInformation))
                                request.AddParameter("custom_fields[cf_how_can_we_help]",
                                    ticketRequest.AdditionalInformation);

                            if (!string.IsNullOrWhiteSpace(ticketRequest.Info))
                                request.AddParameter("custom_fields[cf_please_provide_any_other_additional_details]",
                                    ticketRequest.Info);

                            request.AddParameter("subject",
                                enquireReasonEnum == EnquiryReason.OverdueCashback
                                    ? string.Format(Constant.TicketSubject, "Overdue Cashback", ticketRequest.Store)
                                    : string.Format(Constant.TicketSubject, "Declined Cashback", ticketRequest.Store));

                            break;
                    }

                    request.AddParameter("description", string.Format(description, descriptionFormat));
                    
                    request.AddParameter("custom_fields[cf_what_can_we_help_you_with]",
                        Util.GetDescriptionFromEnum(CashbackProblemHelp.MyRewardsSupport));

                    break;
                }
            }

            var response = await _restClient.ExecuteAsync(request);

            return response.StatusCode == HttpStatusCode.Created;
        }
    }
}