using System.Collections.Generic;

namespace SettingsAPI.Model.Jwt
{
    public class JwtTokenConfiguration
    {
        public JwtTokenConfiguration(string issuer, string issuerSigningKey, List<string> audiences,
            string issuerSigningKeyPassword)
        {
            Issuer = issuer;
            IssuerSigningKey = issuerSigningKey;
            Audiences = audiences;

            IssuerSigningKeyPassword = issuerSigningKeyPassword;
        }

        public JwtTokenConfiguration()
        {
        }

        public string Issuer { get; set; }
        public string IssuerSigningKey { get; set; }
        public List<string> Audiences { get; set; }
        public string IssuerSigningKeyPassword { get; set; }
    }
}