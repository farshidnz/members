using Amazon.SQS;
using Cashrewards3API.Common.Context;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using RestSharp;
using SettingsAPI.BackgroundHostedService;
using SettingsAPI.Common;
using SettingsAPI.Filters;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using SettingsAPI.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Unleash;
using Microsoft.Extensions.Options;
using Unleash.ClientFactory;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

namespace SettingsAPI
{
    public class Startup
    {
        private static ClientCredential _clientCredential;
        public static IConfiguration Configuration { get; private set; }
        public const string ConfigurationName = "Settings";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, _clientCredential);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the access token");
            return result.AccessToken;
        }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            var useDevops4 = Configuration["Devops4Enabled"] == "true";
            var settingsSection = useDevops4 ? Configuration : Configuration.GetSection(ConfigurationName);

            _clientCredential = new ClientCredential(settingsSection["AzureAADClientId"], settingsSection["AzureAADClientSecret"]);

            SqlColumnEncryptionAzureKeyVaultProvider azureKeyVaultProvider =
              new SqlColumnEncryptionAzureKeyVaultProvider(GetToken);

            services.AddMvc(options => options.Filters.Add(typeof(CashRewardsExceptionFilterAttribute)))
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly()));

            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            var redis = new RedisConnectionFactory(settingsSection["RedisMasters"]);
            services.AddSingleton<IDatabase>(s =>
            {
                var conn = redis.GetConnection();
                if (conn != null && conn.IsConnected)
                    return conn.GetDatabase();

                return null;
            });

            services.AddSingleton<IRedisUtil, RedisUtil>();

            string shopGoWriterConnectionString = null;
            string shopGoReaderConnectionString = null;
            if (useDevops4)
            {
                var shopGoHostWriter = settingsSection["SQLServerHostWriter"];
                var shopGoHostReader = settingsSection["SQLServerHostReader"];
                var shopGoDatabase = settingsSection["ShopGoDBName"];
                var shopGoUser = settingsSection["ShopGoDBUser"];
                var shopGoPassword = settingsSection["ShopGoDBPassword"];

                shopGoWriterConnectionString = $"Data Source={shopGoHostWriter};Initial Catalog={shopGoDatabase};User ID={shopGoUser};Pwd={shopGoPassword};Column Encryption Setting=enabled;ENCRYPT=yes;trustServerCertificate=true";
                shopGoReaderConnectionString = $"Data Source={shopGoHostReader};Initial Catalog={shopGoDatabase};User ID={shopGoUser};Pwd={shopGoPassword};Column Encryption Setting=enabled;ENCRYPT=yes;trustServerCertificate=true";
            } 
            else
            {
                shopGoWriterConnectionString = settingsSection["DbConnectionString"];
                shopGoReaderConnectionString = settingsSection["ReadOnlyDbConnectionString"];
            }

            Dictionary<string, SqlColumnEncryptionKeyStoreProvider> providers =
              new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
              {
                  { SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, azureKeyVaultProvider }
              };
            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(providers);
            // Bind our configuration data to the settings class
            services.Configure<Settings>(settingsSection);
            services.AddDbContextPool<Data.ShopGoContext>(options => options.UseSqlServer(shopGoWriterConnectionString));
            services.AddDbContext<Data.ReadOnlyShopGoContext>(options => options.UseSqlServer(shopGoReaderConnectionString));
            services.AddHttpContextAccessor();
            services.AddScoped<IMemberService, MemberService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<IMemberBalanceService, MemberBalanceService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IMemberBankAccountService, MemberBankAccountService>();
            services.AddScoped<IMobileOptService, MobileOtpService>();
            services.AddScoped<IAwsService, AwsService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IMemberClicksHistoryService, MemberClicksHistoryService>();
            services.AddScoped<IMemberPaypalAccountService, MemberPaypalAccountService>();
            services.AddScoped<IMemberRedeemService, MemberRedeemService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPaypalApiService, PaypalApiService>();
            services.AddScoped<ITimeService, TimeService>();
            services.AddScoped<IMemberFavouriteService, MemberFavoriteService>();
            services.AddScoped<IMemberFavouriteCategoryService, MemberFavouriteCategoryService>();
            services.AddScoped<ILeanplumService, LeanplumService>();
            services.AddScoped<IPremiumService, PremiumService>();
            services.AddScoped<IRequestContext, RequestContext>();
            services.AddScoped<IFreshdeskTicketHelperService, FreshdeskTicketHelperService>();
            services.AddScoped<IFreshdeskService, FreshdeskService>();
            services.AddScoped<IRestClient, RestClient>();
            services.AddScoped<IEntityAuditService, EntityAuditService>();
            services.AddScoped<IFieldAuditService, FieldAuditService>();
            services.AddScoped<ITokenValidation, TokenValidationService>();
            services.AddScoped<IAuthorizationHandler, AccessTokenAuthorizationHandler>();
            services.AddScoped<IWebClientFactory, WebClientFactory>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddAuthorization(options =>
            {
                if(useDevops4)
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new AccessTokenRequirement())
                    .Build();
                } 
                else
                {
                    // For backward compatibility with the lambda authorizer ([Authorize] should do nothing in this case)
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true)
                    .Build();
                }
                
            });
            
            services.AddCors();

            services.AddControllers();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddHttpClient();

            services.AddMemoryCache();

            // AWS services
            services.AddAWSService<IAmazonSQS>();
            services.AddSingleton<ISqsServiceFactory, SqsServiceFactory>();

            // Register Hosted Services
            services.AddHostedService<MemberCreatedEventHandlerService>();

            services.AddHealthChecks();

            AddOpenTelementry(services);

            ConfigFeatureToggle(services);
        }

        public void AddOpenTelementry(IServiceCollection services)
        {
            services.AddOpenTelemetryTracing(b =>
            {
                b
                .AddSource("MemberSettingsApi")
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: "MemberSettingsApi", serviceVersion: GetAssemblyVersion())
                        .AddTelemetrySdk())
                .AddXRayTraceId()
                .AddAWSInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    options.Endpoint = new Uri("http://localhost:4317");
                })
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForStoredProcedure = true;
                    options.SetDbStatementForText = true;
                })
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();
            });
        }

        private static string GetAssemblyVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var useDevops4 = Configuration["Devops4Enabled"] == "true";

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                if (!useDevops4) // AccessTokenAuthorizationHandler used when run with devops4 configuration
                {
                    app.Use(async (context, next) =>
                    {
                        var token = context.Request.Headers["Authorization"].ToString().Split(' ')[1];
                        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                        jwt.Payload.TryGetValue("username", out var cognitoId);
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(Constant.CognitoIdClaimPropertyName, cognitoId.ToString()) }));
                        await next();
                    });
                }
            }

            if (useDevops4)
            {
                app.UsePathBase("/api/membersettings");
            }

            app.UseRouting();

            app.UseCors(options => options.SetIsOriginAllowed(origin =>
                origin.EndsWith("cashrewards.com.au"))
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromSeconds(2520)
                )
            );

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health-check");
            });

        }

        private void ConfigFeatureToggle(IServiceCollection services)
        {

            services.AddSingleton<IUnleash>(provider =>
            {
                var config = provider.GetService<IOptions<Settings>>().Value?.UnleashConfig;
                var settings = new UnleashSettings()
                {
                    AppName = config.AppName,
                    UnleashApi = new Uri(config.UnleashApi),
                    Environment = config.Environment,
                    FetchTogglesInterval = TimeSpan.FromMinutes(config.FetchTogglesIntervalMin),
                    CustomHttpHeaders = new Dictionary<string, string>()
                    {
                        ["Authorization"] = config.UnleashApiKey
                    },
                    SendMetricsInterval = TimeSpan.FromSeconds(30)
                };

                var unleashFactory = new UnleashClientFactory();
                IUnleash unleash = unleashFactory.CreateClient(settings, synchronousInitialization: true);
                return unleash;
            });


            services.AddSingleton<IFeatureToggleService, UnleashFeatureToggleService>();
        }

    }
}
