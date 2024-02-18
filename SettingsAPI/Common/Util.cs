using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Security.Cryptography;

namespace SettingsAPI.Common
{
    public class Util
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                    RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static string SanitizeMobilePhone(string phone)
        {
            return Regex.Replace(phone, @"(?:\+)(\d{2})(?:\s)*(0?)(?:\s)*(\d{3})(?:\s)*(\d{3})(?:\s)*(\d{3})", m => $"+{m.Groups[1].Value} {m.Groups[3].Value}{m.Groups[4].Value}{m.Groups[5].Value}");
        }

        public static string ToMaskedMobileNumber(string phone)
        {
            return Regex.Replace(phone, @"(?:\+)(\d{2})(?:\s)*(\d{3})(?:\s)*(\d{3})(?:\s)*(\d{3})", m => $"+{m.Groups[1].Value} *** *** {m.Groups[4].Value}");
        }

        public static async Task<string> ReadAmazonS3Data(string fileKey, string bucket)
        {
            var response = await ReadAmazonS3(fileKey, bucket);
            var responseStream = response.ResponseStream;
            var reader = new StreamReader(responseStream);
            var responseBody = reader.ReadToEndAsync();

            return await responseBody;
        }

        private static async Task<GetObjectResponse> ReadAmazonS3(string fileKey, string bucket)
        {
            //TODO: configure region from settings file 
            var config = new AmazonS3Config {RegionEndpoint = RegionEndpoint.APSoutheast2};

            using var s3Client = new AmazonS3Client(config);
            var request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = fileKey
            };
            var objectResponse = await s3Client.GetObjectAsync(request);

            return objectResponse;
        }

        public static string GetDescriptionFromEnum(Enum value)
        {
            return
                value
                    .GetType()
                    .GetMember(value.ToString())
                    .FirstOrDefault()
                    ?.GetCustomAttribute<DescriptionAttribute>()
                    ?.Description
                ?? value.ToString();
        }

        public static T GetEnumFromDescription<T>(string description) where T : Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T) field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T) field.GetValue(null);
                }
            }

            return default;
        }

        public static string MaskEmail(string email)
        {
            var emailChunk = email.Split("@");
            var emailPre = emailChunk[0];
            var emailSuf = emailChunk[1];

            switch (emailPre.Length)
            {
                case 1:
                    email = new StringBuilder($"*@{emailSuf}").ToString();
                    break;
                case 2:
                {
                    var firstChar = emailPre[0];
                    email = new StringBuilder($"{firstChar}*@{emailSuf}").ToString();
                    break;
                }
                default:
                    email = Regex.Replace(email, Constant.EmailMaskRegex, m => new string('*', m.Length));
                    break;
            }

            return email;
        }

        public static string MaskAllWords(string str)
        {
            return Regex.Replace(str, Constant.MaskRegex, m => new string('*', m.Length));
        }

        public static long GetCurrentTimestamp()
        {
            return (long) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        }

        public static string ConvertPhoneToInternationFormat(string phone)
        {
            if (phone == null)
            {
                return phone;
            }
            if (phone.StartsWith("04"))
            {
                return $"+61 {Regex.Replace(phone[1..].Trim(), @"\s+", "")}";

            }
            if (phone.StartsWith("02"))
            {
                return $"+64 {Regex.Replace(phone[1..].Trim(), @"\s+", "")}";
            }
            return phone;
        }

        public static string ToHashedSurveyEmail(string email, string secret)
        {
            if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(secret))
                throw new Exception("INVALID or NULL MemberEmail or AskNicelySecret");

            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(email);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return System.BitConverter.ToString(hashmessage).Replace("-", string.Empty).ToLower();
            }
        }
    }
}