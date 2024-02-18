using System;

namespace SettingsAPI
{
    public class Settings
    {
        public string DbConnectionString { get; set; }
        public string ReadOnlyDbConnectionString { get; set; }
        public string BsbBucketName { get; set; }

        public string BsbKey { get; set; }

        public string AccountSId { get; set; }

        public string AuthToken { get; set; }


        public string PathServiceSId { get; set; }

        public decimal MinRedemptionAmount { get; set; }

        public decimal MaxRedemptionAmount { get; set; }

        public string PaypalClientId { get; set; }

        public string PaypalClientSecret { get; set; }

        public string PaypalTokenService { get; set; }

        public string PaypalUserInfo { get; set; }

        public string PaypalConnectUrlEndpoint { get; set; }

        public string PaypalScope { get; set; }

        public string MandrillApiEndpoint { get; set; }

        public string MandrillKey { get; set; }

        public string MandrillEmailVerificationTemplateId { get; set; }

        public string MandrillEmailOrphanMobileUpdateTemplateId { get; set; }

        public string WebsiteDomainUrl { get; set; }

        public string MandrillEmailAfterWithdrawSuccessId { get; set; }

        public string OtpWhitelist { get; set; }

        public bool SkipOtp { get; set; }

        public string FeedbackToCustomerId { get; set; }

        public string FeedbackToCashrewardsId { get; set; }

        public string EmailFeedbackToCashrewards { get; set; }

        public string TopicArnMemberUpdatedEvent { get; set; }

        public string TopicArnMemberCreatedEvent { get; set; }

        public string MemberCreatedQueueName { get; set; }

        public string CognitoQueueName { get; set; }

        public string RedisMasters { get; set; }
        public string LeanplumAppId { get; set; }
        public string LeanplumClientKey { get; set; }
        public string OptimiseSmsOptOutKey { get; set; }
        public string SaltKey { get; set; }
        public string MandrillEmailPasswordUpdateTemplateId { get; set; }
        public string FreshdeskApiKey { get; set; }
        public string FreshdeskDomain { get; set; }

        // Toggle for switching between using Linq TransactionViewForMember vs a flattened raw sql
        // which has significant speed improvements but may be forgotten to be updated when views on the database are changed
        public Boolean TransactionMemberViewUseDatabaseView { get; set; }

        public string CognitoTokenIssuerEndpoint { get; set; }
        public string StsTokenIssuerEndpoint { get; set; }
        public bool SendVerificationEmail { get; set; }
        public bool Devops4Enabled { get; set; }

        public UnleashConfig UnleashConfig { get; set; }
        public string AskNicelySecret { get; set; }
    }

    public class UnleashConfig
    {
       public string AppName { get; set; }

      public string UnleashApi { get; set; }

      public string Environment { get; set; }

      public int FetchTogglesIntervalMin { get; set; }

      public string UnleashApiKey { get; set; }
    }
}
