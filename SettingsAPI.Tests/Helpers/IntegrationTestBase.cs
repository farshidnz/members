using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using SettingsAPI.Data;
using System.Collections.Generic;

namespace SettingsAPI.Tests.Helpers
{
    public class IntegrationTestBase
    {
        public ShopGoContext Context { get; }

        private IConfiguration _configuration;

        public IntegrationTestBase()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("local.json")
                .Build();

            var options = new DbContextOptionsBuilder<ShopGoContext>()
                .UseSqlServer(_configuration["Settings:DbConnectionString"])
                .Options;

            Context = new ShopGoContext(options);

            EnableColumnEncryption();
        }

        private static bool _columnEncryptionEnabled = false;

        private void EnableColumnEncryption()
        {
            if (_columnEncryptionEnabled) return;
            _columnEncryptionEnabled = true;

            var credentials = new ClientCredential(_configuration["Settings:AzureAADClientId"], _configuration["Settings:AzureAADClientSecret"]);

            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
            {
                [SqlColumnEncryptionAzureKeyVaultProvider.ProviderName] = new SqlColumnEncryptionAzureKeyVaultProvider(async (string authority, string resource, string scope) =>
                    (await new AuthenticationContext(authority).AcquireTokenAsync(resource, credentials)).AccessToken
                )
            });
        }
    }
}
