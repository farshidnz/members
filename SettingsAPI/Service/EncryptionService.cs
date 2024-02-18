using System;
using System.Security.Cryptography;
using System.Text;

namespace SettingsAPI.Service
{
    public class EncryptionService : IEncryptionService
    {
        public string EncryptWithSalt(string input, string salt)
        {
            try
            {
                return GetSHA256Hash(input, salt);
            }
            catch
            {
                return null;
            }
        }

        public string GenerateSaltKey(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public string ComputeSha256Hash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (var i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public string Base64Encode(string variable1, string variable2)
        {
            return Convert.ToBase64String(
                Encoding.ASCII.GetBytes(
                    $"{variable1}:{variable2}"));
        }
        
        public string Base64Decode(string base64EncodedVariable)
        {
            var data = Convert.FromBase64String(base64EncodedVariable);
            return Encoding.ASCII.GetString(data);
        }

        private string GetSHA256Hash(string s, string t)
        {
            var salt = Encoding.UTF8.GetBytes(t);
            var data = Encoding.UTF8.GetBytes(s);

            var plainTextWithSaltBytes = new byte[data.Length + salt.Length];
            for (var i = 0; i < data.Length; i++)
            {
                plainTextWithSaltBytes[i] = data[i];
            }

            for (var i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[data.Length + i] = salt[i];
            }

            var hash = new SHA256CryptoServiceProvider().ComputeHash(plainTextWithSaltBytes);
            return Convert.ToBase64String(hash);
        }
    }
}