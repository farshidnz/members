using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using SettingsAPI.Common;
using SettingsAPI.Model.Enum;

namespace SettingsAPI.Model.Rest.CreateTicket
{
    public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
    {

        public CreateTicketRequestValidator()
        {
            RuleFor(field => field.Contact.Trim())
                .NotEmpty()
                .WithMessage(string.Format(AppMessage.FieldRequired, "Contact"))
                .EmailAddress()
                .WithMessage("Contact must be a email");
            
            RuleFor(field => field.FirstName)
                .NotEmpty()
                .WithMessage(string.Format(AppMessage.FieldRequired, "FirstName"))
                .Must(ValidateFirstName)
                .WithMessage(string.Format(AppMessage.NameInvalid, "FirstName"));
            
            RuleFor(field => field.LastName)
                .NotEmpty()
                .WithMessage(string.Format(AppMessage.FieldRequired, "LastName"))
                .Must(ValidateLastName)
                .WithMessage(string.Format(AppMessage.NameInvalid, "LastName"));

            RuleFor(field => field.OrderConfirmationOrInvoices)
                .Must(ValidateOrderConfirmationOrInvoicesFileType)
                .When(field => field.OrderConfirmationOrInvoices != null && field.OrderConfirmationOrInvoices.Count > 0)
                .WithMessage(AppMessage.AttachmentTypeNotSupport)
                .Must(ValidateOrderConfirmationOrInvoicesSize)
                .When(field => field.OrderConfirmationOrInvoices != null && field.OrderConfirmationOrInvoices.Count > 0)
                .WithMessage(AppMessage.TicketAttachmentOutOfSize);

            RuleFor(field => field.DateOfPurchase)
                .Must(ValidateDateOfPurchase)
                .WithMessage(string.Format(AppMessage.DateInvalid, "DateOfPurchase"));

            RuleFor(field => field.SaleValueTracked)
                .Must(ValidateFieldIsNumeric)
                .When(field => !string.IsNullOrWhiteSpace(field.SaleValueTracked))
                .WithMessage(AppMessage.FieldMustBeNumber);
            
            RuleFor(field => field.PremiumMember)
                .NotEmpty()
                .WithMessage(string.Format(AppMessage.FieldRequired, "PremiumMember"))
                .Must(ValidatePremiumMember)
                .WithMessage(string.Format(AppMessage.FieldInvalid, "PremiumMember"));
            
            RuleFor(field => field.EnquiryReason)
                .NotEmpty()
                .WithMessage(string.Format(AppMessage.FieldRequired, "EnquiryReason"))
                .Must(ValidateEnquiryReason)
                .WithMessage(string.Format(AppMessage.FieldInvalid, "EnquiryReason"));
            
            RuleFor(field => field.IsApprovalDatePass)
                .Must(ValidateIsApprovalDatePass)
                .When(field => !string.IsNullOrWhiteSpace(field.IsApprovalDatePass))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "IsApprovalDatePass"));
            
            RuleFor(field => field.SaleValueExcepted)
                .Must(ValidateFieldIsNumeric)
                .When(field => !string.IsNullOrWhiteSpace(field.SaleValueExcepted))
                .WithMessage(string.Format(AppMessage.FieldMustBeNumber, "SaleValueExcepted"));
            

            RuleFor(field => field.ApplyDiscounts)
                .Must(ValidateApplyDiscounts)
                .When(field => !string.IsNullOrWhiteSpace(field.ApplyDiscounts))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "ApplyDiscounts"));
            
            RuleFor(field => field.DiscountType)
                .Must(ValidateDiscountType)
                .When(field => !string.IsNullOrWhiteSpace(field.DiscountType))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "DiscountType"));
            
            RuleFor(field => field.PurchaseApp)
                .Must(ValidatePurchaseApp)
                .When(field => !string.IsNullOrWhiteSpace(field.PurchaseApp))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "PurchaseApp"));
            
            RuleFor(field => field.BonusIssue)
                .Must(ValidateBonusIssue)
                .When(field => !string.IsNullOrWhiteSpace(field.BonusIssue))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "BonusIssue"));
            
            RuleFor(field => field.IsProceed)
                .Must(ValidateIsProceed)
                .When(field => !string.IsNullOrWhiteSpace(field.IsProceed))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "IsProceed"));

            RuleFor(field => field.TransferDurationPassed)
                .Must(ValidateTransferDurationPassed)
                .When(field => !string.IsNullOrWhiteSpace(field.TransferDurationPassed))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "TransferDurationPassed"));
            
            RuleFor(field => field.PurchaseApproved)
                .Must(ValidatePurchaseApproved)
                .When(field => !string.IsNullOrWhiteSpace(field.PurchaseApproved))
                .WithMessage(string.Format(AppMessage.FieldInvalid, "PurchaseApproved"));
        }
        
        private static bool ValidateFirstName(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return false;
            
            return firstName.Length > 1 && firstName.Length <= 50;
        }

        private static bool ValidateLastName(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                return false;
            
            return lastName.Length > 1 && lastName.Length <= 50;
        }

        private static bool ValidateOrderConfirmationOrInvoicesFileType(IFormFileCollection fileCollection)
        {

            var validEnumFileTypes = new DescriptionAttributes<OrderConfirmationOrInvoiceFileType>().Descriptions.ToList();
                
            //Check file type
            return fileCollection.Select(file => file.ContentType).All(type => validEnumFileTypes.Contains(type));
        }

        private static bool ValidateOrderConfirmationOrInvoicesSize(IFormFileCollection fileCollection)
        {
            return !fileCollection.Any(file => file.Length > Constant.MaxFileSizeOfTicket);
        }

        private static bool ValidateDateOfPurchase(string dateOfPurchase)
        {
            if (string.IsNullOrWhiteSpace(dateOfPurchase))
                return true;
            
            try
            {
               DateTime.ParseExact(dateOfPurchase, Constant.DateQueryParameterFormat,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool ValidatePremiumMember(string premiumMember)
        {
            if (string.IsNullOrWhiteSpace(premiumMember))
                return false;
            
            var validPremiumMembersDescription = new DescriptionAttributes<PremiumMember>().Descriptions.ToList();
            return validPremiumMembersDescription.Contains(premiumMember);
        }

        private static bool ValidateEnquiryReason(string enquiryReason)
        {
            if (string.IsNullOrWhiteSpace(enquiryReason))
                return false;
            
            var enquiryReasonsValidDescription = new DescriptionAttributes<EnquiryReason>().Descriptions.ToList();
            return enquiryReasonsValidDescription.Contains(enquiryReason);
        }

        private static bool ValidateIsApprovalDatePass(string isApprovalDatePass)
        {
            var isApprovalDatePassesValidDescription = new DescriptionAttributes<ApprovalDatePass>().Descriptions.ToList();
            return isApprovalDatePassesValidDescription.Contains(isApprovalDatePass);
        }

        private static bool ValidateFieldIsNumeric(string value)
        {
            return Information.IsNumeric(value);
        }

        private static bool ValidateApplyDiscounts(string applyDiscounts)
        {
            var validEnumApplyDiscountsDescription = new DescriptionAttributes<ApplyDiscounts>().Descriptions.ToList();
            return validEnumApplyDiscountsDescription.Contains(applyDiscounts);
        }

        private static bool ValidateDiscountType(string discountType)
        {
            var validEnumDiscountTypesDescription = new DescriptionAttributes<DiscountType>().Descriptions.ToList();
            return validEnumDiscountTypesDescription.Contains(discountType);
        }

        private static bool ValidatePurchaseApp(string purchaseApp)
        {
            var validEnumPurchaseAppsDescription = new DescriptionAttributes<PurchaseApp>().Descriptions.ToList();
            return validEnumPurchaseAppsDescription.Contains(purchaseApp);
        }

        private static bool ValidateBonusIssue(string bonusIssue)
        {
            var validEnumBonusIssueDescription = new DescriptionAttributes<BonusIssue>().Descriptions.ToList();
            return validEnumBonusIssueDescription.Contains(bonusIssue);
        }

        private static bool ValidateIsProceed(string isProceed)
        {
            var validEnumProceedDescription = new DescriptionAttributes<ProceedType>().Descriptions.ToList();
            return validEnumProceedDescription.Contains(isProceed);
        }

        private static bool ValidateTransferDurationPassed(string transferDurationPassed)
        {
            var validEnumTransferDurationPassedDescription =
                new DescriptionAttributes<TransferDurationPassedState>().Descriptions.ToList();
            return validEnumTransferDurationPassedDescription.Contains(transferDurationPassed);
        }

        private static bool ValidatePurchaseApproved(string purchaseApproved)
        {
            var validEnumPurchaseApprovedDescription =
                new DescriptionAttributes<PurchaseApproved>().Descriptions.ToList();
            return validEnumPurchaseApprovedDescription.Contains(purchaseApproved);
        }

        private class DescriptionAttributes<T>
        {
            private readonly List<DescriptionAttribute> _attributes = new List<DescriptionAttribute>();
            internal List<string> Descriptions { get; }
 
            public DescriptionAttributes()
            {
                RetrieveAttributes();
                Descriptions = _attributes.Select(x => x.Description).ToList();
            }

            private void RetrieveAttributes()
            {
                foreach (var attribute in typeof(T).GetMembers().SelectMany(member =>
                    member.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>()))
                    _attributes.Add(attribute);
            }
        }

    }
    
}