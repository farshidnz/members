import { Duration, Fn, Stack, StackProps } from "aws-cdk-lib";
import {
  getEnv,
  getResourceName,
  EcsConstruct,
  ServiceVisibility,
  RestApiConstruct,
} from "@cashrewards/cdk-lib";
import { Effect, IRole, ManagedPolicy, PolicyDocument, PolicyStatement, Role, ServicePrincipal } from "aws-cdk-lib/aws-iam";
import { SqsSubscription } from "aws-cdk-lib/aws-sns-subscriptions";
import { Topic } from "aws-cdk-lib/aws-sns";
import { Queue } from "aws-cdk-lib/aws-sqs";
import { Construct } from "constructs";
import { getegid } from "process";
import { HttpIntegration, Integration, IntegrationType, RestApi, SecurityPolicy } from "aws-cdk-lib/aws-apigateway";
import { Certificate } from "aws-cdk-lib/aws-certificatemanager";
import { ARecord, HostedZone, RecordTarget } from "aws-cdk-lib/aws-route53";
import { ApiGateway } from "aws-cdk-lib/aws-route53-targets";

export class DeployStack extends Stack {

  private memberCreatedEventQueue: Queue;
  public restApi: RestApi;

  constructor(scope: Construct, id: string, props?: StackProps) {
    super(scope, id, props);

    const memberCreatedQueueName = getResourceName("memberCreatedEventQueue");
    this.memberCreatedEventQueue = new Queue(this, memberCreatedQueueName, {
      queueName: memberCreatedQueueName,
      receiveMessageWaitTime: Duration.seconds(20),
      visibilityTimeout: Duration.minutes(10),
      retentionPeriod: Duration.hours(24),
    });

    const memberCreatedEventQueueSnsSubscription = new SqsSubscription(
      this.memberCreatedEventQueue,
      {
        rawMessageDelivery: true
      }
    );

    Topic.fromTopicArn(
      this,
      getResourceName("memberCreatedEventQueueSnsSubscription"),
      getEnv("TopicArnMemberCreatedEvent")
    ).addSubscription(memberCreatedEventQueueSnsSubscription);

    new EcsConstruct(this, getResourceName("ecs"), {
      environmentName: getEnv("ENVIRONMENT_NAME"),
      serviceName: getEnv("PROJECT_NAME"),
      pathPattern: "api/membersettings",
      healthCheckPath: "api/membersettings/health-check",
      visibility: ServiceVisibility.PUBLIC,
      listenerRulePriority: 400,
      imageTag: getEnv("VERSION"),
      taskRole: this.getRole(),
      customDomain: 'member-api',
      alternateDomains: [ getEnv("AlternateDNS") ],
      minCapacity: +getEnv("ScalingMinCapacity"),
      maxCapacity: +getEnv("ScalingMaxCapacity"),
      desiredCount: +getEnv("ScalingDesiredCount"),
      cpu: +getEnv("cpu"),
      memoryLimitMiB: +getEnv("memoryLimitMiB"),
      scalingRule: {
        cpuScaling: {
          targetUtilizationPercent: 70,
          scaleInCooldown: 300,
          scaleOutCooldown: 300,
          alarm: {
            enableSlackAlert: true
          }
        },
        memoryScaling: {
          targetUtilizationPercent: 70,
          scaleInCooldown: 300,
          scaleOutCooldown: 300,
          alarm: {
            enableSlackAlert: true
          }
        },
      },
      environment: {
        Environment: getEnv("Environment"),
        AccountSId: getEnv("TwilioAccountSid"),
        BsbBucketName: getEnv("BsbBucketName"),
        BsbKey: getEnv("BsbKey"),
        CognitoQueueARN: getEnv("CognitoQueueARN"),
        CognitoQueueName: getEnv("CognitoQueueName"),
        CognitoTokenIssuerEndpoint: getEnv("CognitoTokenIssuerEndpoint"),
        ConnectionArn: getEnv("ConnectionArn"),
        CorsAllowDomain: getEnv("CorsAllowDomain"),
        EmailFeedbackToCashrewards: getEnv("EmailFeedbackToCashrewards"),
        FeedbackToCashrewardsId: getEnv("FeedbackToCashrewardsId"),
        FeedbackToCustomerId: getEnv("FeedbackToCustomerId"),
        FreshdeskApiKey: getEnv("FreshdeskApiKey"),
        FreshdeskDomain: getEnv("FreshdeskDomain"),
        LeanplumAppId: getEnv("LeanplumAppId"),
        LeanplumClientKey: getEnv("LeanplumClientKey"),
        MandrillApiEndpoint: getEnv("MandrillApiEndpoint"),
        MandrillEmailAfterWithdrawSuccessId: getEnv(
          "MandrillEmailAfterWithdrawSuccessId"
        ),
        MandrillEmailOrphanMobileUpdateTemplateId: getEnv(
          "MandrillEmailOrphanMobileUpdateTemplateId"
        ),
        MandrillEmailPasswordUpdateTemplateId: getEnv(
          "MandrillEmailPasswordUpdateTemplateId"
        ),
        MandrillEmailVerificationTemplateId: getEnv(
          "MandrillEmailVerificationTemplateId"
        ),
        MaxRedemptionAmount: getEnv("MaxRedemptionAmount"),
        MemberCreatedQueueName: memberCreatedQueueName,
        MinRedemptionAmount: getEnv("MinRedemptionAmount"),
        OtpWhitelist: getEnv("OtpWhitelist"),
        SkipOtp: getEnv("SkipOtp"),
        PathServiceSId: getEnv("PathServiceSId"),
        PaypalClientId: getEnv("PaypalClientId"),
        PaypalConnectUrlEndpoint: getEnv("PaypalConnectUrlEndpoint"),
        PaypalScope: getEnv("PaypalScope"),
        PaypalTokenService: getEnv("PaypalTokenService"),
        PaypalUserInfo: getEnv("PaypalUserInfo"),
        RedisMasters: getEnv("RedisMasters"),
        SaltKey: getEnv("SaltKey"),
        StsTokenIssuerEndpoint: getEnv("StsTokenIssuerEndpoint"),
        TopicArnMemberCreatedEvent: getEnv("TopicArnMemberCreatedEvent"),
        TopicArnMemberUpdatedEvent: getEnv("TopicArnMemberUpdatedEvent"),
        WebsiteDomainUrl: getEnv("WebsiteDomainUrl"),
        SQLServerHostWriter: getEnv("SQLServerHostWriter"),
        SQLServerHostReader: getEnv("SQLServerHostReader"),
        ShopGoDBName: getEnv("ShopGoDBName"),
        ShopGoDBUser: getEnv("ShopGoDBUser"),
        SendVerificationEmail: getEnv("SendVerificationEmail"),
        UnleashConfig__AppName: getEnv("UnleashAppName"),
        UnleashConfig__UnleashApi: getEnv("UnleashApi"),
        UnleashConfig__Environment: getEnv("UnleashEnvironment"),
        UnleashConfig__FetchTogglesIntervalMin: getEnv("UnleashFetchTogglesIntervalMin")
      },
      secrets: {
        AuthToken: getEnv("AuthToken"),
        AzureAADClientId: getEnv("AzureAADClientId"),
        AzureAADClientSecret: getEnv("AzureAADClientSecret"),
        DbConnectionString: getEnv("DbConnectionString"),
        MandrillKey: getEnv("MandrillKey"),
        OptimiseSmsOptOutKey: getEnv("OptimiseSmsOptOutKey"),
        PaypalClientSecret: getEnv("PaypalClientSecret"),
        ShopGoDBPassword: getEnv("ShopGoDBPassword"),
        UnleashConfig__UnleashApiKey: getEnv("UnleashApiKey"),
        AskNicelySecret: getEnv("AskNicelySecret")
      },
      useOpenTelemetry: true
    });
  }

  private getRole(): IRole {
    
    const taskDefinitionRole = new Role(
      this,
      getResourceName("memberSettingsTaskRole"),
      {
        assumedBy: new ServicePrincipal("ecs-tasks.amazonaws.com"),
      }
    );

    taskDefinitionRole.addManagedPolicy({
      managedPolicyArn: "arn:aws:iam::aws:policy/AmazonS3ReadOnlyAccess",
    });

    const policyDoc = new PolicyDocument();

    policyDoc.addStatements(
      new PolicyStatement({
        actions: [
          "sqs:*",
        ],
        resources: [this.memberCreatedEventQueue.queueArn, getEnv("CognitoQueueARN")],
        effect: Effect.ALLOW,
      })
    );

    policyDoc.addStatements(
      new PolicyStatement({
        actions: [
          "sns:Publish",
        ],
        resources: [getEnv("TopicArnMemberUpdatedEvent")],
        effect: Effect.ALLOW,
      })
    );

    // const policy = new Policy(this, getResourceName('ecsNginxRolePolicy'))
    const policy = new ManagedPolicy(this, getResourceName("managedPolicy"), {
      document: policyDoc,
    });

    policy.attachToRole(taskDefinitionRole);
    return taskDefinitionRole;
  }

}
