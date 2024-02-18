namespace SettingsAPI.Common
{
    public class AppMessage
    {
        public const string Unauthorized = "Unauthorized";
        public const string ApiRequestInvalid = "Invalid request";
        public const string FieldInvalid = "{0} is invalid";
        public const string ObjectNotFound = "{0} not found";
        public const string ApiResponseStatusUpdated = "UPDATED";
        public const string ApiResponseStatusLinked = "LINKED_{0}";
        public const string ApiResponseStatusInserted = "INSERTED";
        public const string ApiResponseStatusCreated = "CREATED";
        public const string ApiResponseStatusInvalid = "INVALID_{0}";
        public const string ApiResponseStatusNotFound = "NOT_FOUND";
        public const string ApiResponseStatusDuplicated = "DUPLICATED";
        public const string ApiResponseStatusErrorInternal = "INTERNAL_SERVER_ERROR";
        public const string ApiResponseStatusSent = "SENT";
        public const string ApiResponseStatusNoAvailableBalance = "NO_AVAILABLE_BALANCE";
        public const string ApiResponseStatusMemberNotRedeem = "MEMBER_NOT_REDEEM";
        public const string ApiResponseStatusDeleted = "DELETED";
        public const string ApiResponseStatusUnverified = "UNVERIFIED_{0}";
        public const string ApiResponseStatusVerified = "VERIFIED_{0}";
        public const string ApiResponseStatusExpired = "EXPIRED_{0}";
        public const string ApiResponseStatusUnauthorized = "UNAUTHORIZED";
        public const string ApiResponseStatusValid = "VALID_{0}";
        public const string ApiResponseStatusAlreadyInUsed = "{0}_IS_IN_USED";
        public const string ApiResponseStatusOk = "SUCCESS";

        public const string UpdateObjectSuccessful = "{0} has been updated";
        public const string GenericPreferencesUpdated = "You have successfully updated your preferences";
        public const string BankAccountDisconnectSuccessful = "Your bank account has been unlinked";
        public const string AccountCloseSuccessful = "Your account has been closed";
        public const string PaypalAccountUnlinkSuccessful = "Your PayPal account has been unlinked";
        public const string ApprovedOnceYourQualifyingConfirmed = "Approved once your qualifying purchase is confirmed";
        public const string ApprovedOnceYourFriendQualifyingConfirmed =
            "Approved once your friend's qualifying purchase is confirmed";
        public const string BankAccountDuplicate =
            "This bank account is already associated with more than two accounts";
        public const string OtpSuccessMessage = "The verification code has been sent to {0}";
        public const string MemberNoAvailableBalance = "You have no available balance";
        public const string MinimumAmountRequired = "Minimum amount of ${0} is required";
        public const string AmountGreaterThanAvailableRewards =
            "The amount requested must be equal to or less than your Available Rewards";
        public const string AmountGreaterThanMaximumLimit = "The amount exceeds the maximum limit";
        public const string PaypalAccountNotVerify =
            "Your PayPal account needs to be verified before you can withdraw from Cashrewards " +
            @"<a href=""https://www.paypal.com/au/smarthelp/article/how-do-i-verify-my-paypal-account-faq444"">more info</a>";
        public const string MemberNotRedeem = "At least one approved purchase is required to submit a payment request";
        public const string WithdrawSuccess = "Your withdrawal was successful";
        public const string PaypalAccountDuplicate =
            "This PayPal account is already associated with more than two accounts";
        public const string PaypalAuthorizationCodeUnauthorized = "PayPal authorization code is invalid";
        public const string PaypalAccountLinkSuccessful = "Your PayPal account has been linked";
        public const string EmailVerified = "Your email has been verified";
        public const string EmailVerificationSent = "Verification email has been sent";
        public const string EmailOrphanMobileUpdatingSent = "Update account email has been sent";
        public const string TokenExpired = "Token has expired";
        public const string EmailAfterWithdrawSuccess = "Withdraw confirmation email has been sent";
        public const string MobileAlreadyInUse = "Mobile number is already in use";
        public const string EmailAlreadyInUse = "That email is already in use";
        public const string NotifierStatusMessage = "Notifier status has been changed.";
        public const string MobileOtpIncorrect = "The code you entered is incorrect. Please try again.";
        public const string DateOfBirthWrong = "You must be at least 14 years old to have an account";
        public const string DateOfBirthNotAllow = "You must be at least 14 years old to have an account";
        public const string FeedbackSent = "The feedback email has been sent";
        public const string FavAdded = "Added to favourites";
        public const string FavDeleted = "Removed from favourites";
        public const string CommsPromptShown = "Communications prompt count updated";
        public const string LinkIsValid = "The link is valid";

        public const string CreateTicketSuccessful = "Your ticket was created successful";
        public const string TicketAttachmentOutOfSize =
            "Your ticket attachment is too big, please choose the file with a size less than equal 20Mb";
        public const string FieldMustBeNumber= "{0} must be a number";
        public const string FieldRequired = "{0} is required";
        public const string NameInvalid = "{0} must have length greater than 1 and less than equal 50";
        public const string DateInvalid = "{0} must be formated like yyyy-MM-dd";

        public const string AttachmentTypeNotSupport =
            "Your ticket attachment type is not support, please try again with other type";
        public const string CreateTicketError = "Something went wrong when creating the ticket, please try again";
    }
}
