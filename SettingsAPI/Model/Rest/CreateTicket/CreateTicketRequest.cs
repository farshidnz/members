using Microsoft.AspNetCore.Http;

namespace SettingsAPI.Model.Rest.CreateTicket
{
    public class CreateTicketRequest
    {
        public string Contact { get; set; }
        
        //Custom fields
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        public IFormFileCollection OrderConfirmationOrInvoices { get; set; }
        
        //Tracked 
        public string DateOfPurchase { get; set; }
        public string Store { get; set; }
        public string SaleValueTracked { get; set; }
        public string Cashback { get; set; }
        public string EstimateApprovalTimeframe { get; set; }
        //Expected
        public string SaleValueExcepted { get; set; }
        
        public string CashbackOptional { get; set; }
        public string OrderId { get; set; }

        public string PremiumMember { get; set; }
        
        public string EnquiryReason { get; set; }
        
        public string IsApprovalDatePass { get; set; }
        
        public string Info { get; set; }
        
        public string ApplyDiscounts { get; set; }
        
        public string DiscountType { get; set; }
        
        public string DiscountCode { get; set; }
        
        public string PurchaseApp { get; set; }

        public string BonusIssue { get; set; }
        
        public string IsProceed { get; set; }
        
        public string AdditionalInformation { get; set; }
        
        public string TransferDurationPassed { get; set; }
        
        public string PurchaseApproved { get; set; }

        public int TransactionId { get; set; }
    }
}