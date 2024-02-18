namespace SettingsAPI.Common
{
    public class Constant
    {
        public const string DateOfBirthFormat = "yyyy-MM-dd";
        public const string DateQueryParameterFormat = "yyyy-MM-dd";
        public const string DatePrintFormat = "dd-MM-yyyy";
        public const string WelcomeBonusStringPrefix = "WelcomeBonus-";
        public const string SaltCharacterReplace1 = "S1U3L9P";
        public const string SaltCharacterReplace2 = "S1L3A9S";
        public const string SaltCharacterReplace3 = "F7L4S0H";
        public const int DefaultLimit = 20;
        public const int DefaultOffset = 0;
        public const string DefaultOrderByField = "Date";
        public const string DefaultSortDirection = "Desc";
        public const int CashRewardsReferAMateMerchantId = 1002347;
        public const int CashRewardsBonusMerchantId = 1001211;
        public const int CashRewardsWelcomeBonusMerchantId = 1004708;
        public const int CashRewardsPromotionalBonusMerchantId = 1004709;
        public const string MemberIdClaimPropertyName = "memberId";
        public const string CognitoIdClaimPropertyName = "cognitoId";

        public const string AustraliaPhoneRegex =
            @"^\({0,1}((\+61)(\ |)(0){0,1}(4)){0,1}\){0,1}[0-9]{8}$";

        public const string NewzealandPhoneRegex = @"^\({0,1}((\+64)(\ |)(0){0,1}(2)){0,1}\){0,1}[0-9]{7,9}$";
        public const string MobileObfuscateRegex = @"(?'cCode'\+\d{2})\s" +
                                                    @"(?'first2digits'\d{2})" +
                                                    @"(?'centralDigits'\d{4})" +
                                                    @"(?'trailingDigits'\d{3})";

        public const string PostCodeRegex = "^[0-9]{4}$";
        public const string PostCodeObfuscateRegex = @"(?'first2Digits'\d{2})(?'trailingDigits'\d{2})";
        public const string AccountNumberRegex = "[1-9]";
        public const int PasswordMinLength = 8;
        public const int EmailMaxLength = 200;

        public const string UriRegex =
            @"(?:^|\s)((https?:\/\/)|(cashrewards?:\/\/)?(?:paypal|[\w-]+(?:\.[\w-]+)+)(:\d+)?(\/\S*)?)";

        public const string MaskRegex = @"(?<=[\w]{1})[\w]*(?=[\w]{1})";
        public const string MobileLinkCode = @"^\d+\$\d+\$\S+$";
        public const string EmailMaskRegex = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";
        public const int TokenTimeToLive = 86400; // = 24 hours
        public const string VerifyEmailApiEndpoint = "api/member/verify-email";
        public const string UpdateMobileWithCodeApiEndpoint = "api/member/update-mobile-with-code";
        public const string VerifyEmailToken = "x-verify-email";
        public const string MobileUpdateWithCodeToken = "x-orphan-mobile-update";
        public const int MemberIdForUnAuthorized = -1;

        //Minimum user age allow use this system
        public const int MinUserAgeAllow = 14;

        public const string AccountNameRegex = "^[a-z A-Z]+$";

        public const int MaxCommsPromptShownCount = 2;

        public const string ShopGoBaseUrl = "https://cms.shopgo.com.au";
        public const long MaxFileSizeOfTicket = 20 * 1024 * 1024;
        public const string TicketSubject = "{0} - {1}";
        public const string RegexEmailWhiteList = @"^qa\+signup.*@cashrewards\.com$";
        public const string RegexMobileBypassList = @"^(\+61 400000004$)";

        public static class Clients
        {
            public const int CashRewards = 1000000;
            public const int MoneyMe = 1000033;
            public const int ANZ = 1000034;
        }

        public static class PremiumStatus
        {
            public const int NotEnrolled = 0;
            public const int Enrolled = 1;
            public const int OptOut = 999;
        }

        public static class ClientGroup
        {
            public static int[] ANZPremium => new int[] { Clients.CashRewards, Clients.ANZ };
        }

        public static class Networks
        {
            public const int InStoreNetwork = 1000053;
        }

        public static class Mapper
        {
            public const string PersonId = "PersonId";
            public const string MemberId = "MemberId";
            public const string CommsPromptDismissalAction = "Action";
        }
    }
}