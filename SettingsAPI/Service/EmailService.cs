using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SettingsAPI.Common;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;

namespace SettingsAPI.Service
{
    public class EmailService : IEmailService
    {
        private readonly IOptions<Settings> _options;
        private readonly IEncryptionService _encryptionService;

        public EmailService(IOptions<Settings> options, IEncryptionService encryptionService)
        {
            _options = options;
            _encryptionService = encryptionService;
        }

        public async Task SendVerificationEmail(int memberId, string firstName, string mailTo, string hashedEmail)
        {
            var mandrillTemplateId = _options.Value.MandrillEmailVerificationTemplateId;
            var mandrillKey = _options.Value.MandrillKey;
            var webSiteDomainUrl = _options.Value.WebsiteDomainUrl;

            var verificationLink = new StringBuilder().Append(webSiteDomainUrl).Append("/verify-your-email")
                .Append("?").Append("code=").Append($"{memberId}${hashedEmail}")
                .ToString();

            var body = new MandrillEmailBodyRequest
            {
                Key = mandrillKey,
                TemplateName = mandrillTemplateId,
                Async = true,
                Message = new Message
                {
                    To = new List<To>
                    {
                        new To
                        {
                            Email = mailTo,
                            Name = firstName,
                            Type = "to"
                        }
                    },
                    GlobalMergeVars = new List<GlobalMergeVar>
                    {
                        new GlobalMergeVar
                        {
                            Name = "FirstName",
                            Content = firstName
                        },
                        new GlobalMergeVar
                        {
                            Name = "VerificationLink",
                            Content = verificationLink
                        }
                    },
                    MergeLanguage = Util.GetDescriptionFromEnum(MergeLanguage.HandleBars)
                }
            };

            await SendTemplateMail(body);
        }

        public async Task SendOrphanMobileEmail(int memberId, string firstName, string mailTo, string mobile)
        {
            var mandrillTemplateId = _options.Value.MandrillEmailOrphanMobileUpdateTemplateId;
            var mandrillKey = _options.Value.MandrillKey;
            var webSiteDomainUrl = _options.Value.WebsiteDomainUrl;
            var saltKey = _options.Value.SaltKey;

            var currentTimestamp = Util.GetCurrentTimestamp();

            var data = new StringBuilder().Append(memberId).Append("|").Append(mobile).Append("|")
                .Append(currentTimestamp).ToString();

            var code = _encryptionService.EncryptWithSalt(data, saltKey)
                .Replace("+", Constant.SaltCharacterReplace1)
                .Replace("/", Constant.SaltCharacterReplace2)
                .Replace(@"\", Constant.SaltCharacterReplace3);

            var linkCode = $"{memberId}${currentTimestamp}${code}";

            var verificationLinkBuilder = new StringBuilder().Append(webSiteDomainUrl).Append("/update-your-mobile")
                .Append("?").Append("code=").Append(linkCode);

            string headingText;
            string buttonText;
            string bodyText;
            string subject;

            if (string.IsNullOrWhiteSpace(mobile))
            {
                headingText = "Add your mobile number";
                buttonText = "Add mobile number";
                verificationLinkBuilder.Append("&action=add");
                bodyText = "add";
                subject = "Add your mobile number";
            }
            else
            {
                headingText = "Update your mobile number";
                buttonText = "Update mobile number";
                verificationLinkBuilder.Append("&action=update");
                bodyText = "update";
                subject = "Update your mobile number";
            }

            var verificationLink = verificationLinkBuilder.ToString();

            var body = new MandrillEmailBodyRequest
            {
                Key = mandrillKey,
                TemplateName = mandrillTemplateId,
                Async = true,
                Message = new Message
                {
                    To = new List<To>
                    {
                        new To
                        {
                            Email = mailTo,
                            Name = firstName,
                            Type = "to"
                        }
                    },
                    Subject = subject,
                    GlobalMergeVars = new List<GlobalMergeVar>
                    {
                        new GlobalMergeVar
                        {
                            Name = "FirstName",
                            Content = firstName
                        },
                        new GlobalMergeVar
                        {
                            Name = "UpdateMobileLink",
                            Content = verificationLink
                        },
                        new GlobalMergeVar
                        {
                            Name = "HeadingText",
                            Content = headingText
                        },
                        new GlobalMergeVar
                        {
                            Name = "ButtonText",
                            Content = buttonText
                        },
                        new GlobalMergeVar
                        {
                            Name = "BodyText",
                            Content = bodyText
                        }
                    },
                    MergeLanguage = Util.GetDescriptionFromEnum(MergeLanguage.HandleBars)
                }
            };

            await SendTemplateMail(body);
        }

        public async Task SendEmailAfterWithdrawSuccess(string mailTo, string firstName, decimal amount, string paymentMethod)
        {
            var mandrillTemplateId = _options.Value.MandrillEmailAfterWithdrawSuccessId;
            var mandrillKey = _options.Value.MandrillKey;

            var body = new MandrillEmailBodyRequest
            {
                Key = mandrillKey,
                TemplateName = mandrillTemplateId,
                Async = true,
                Message = new Message
                {
                    To = new List<To>
                    {
                        new To
                        {
                            Email = mailTo,
                            Name = firstName,
                            Type = "to"
                        }
                    },
                    GlobalMergeVars = new List<GlobalMergeVar>
                    {
                        new GlobalMergeVar
                        {
                            Name = "FirstName",
                            Content = firstName
                        },
                        new GlobalMergeVar
                        {
                            Name = "Amount",
                            Content = amount.ToString("0.00")
                        },
                        new GlobalMergeVar
                        {
                            Name = "PaymentMethod",
                            Content = paymentMethod
                        }
                    },
                    MergeLanguage = Util.GetDescriptionFromEnum(MergeLanguage.HandleBars)
                }
            };

            await SendTemplateMail(body);
        }

        public async Task SendFeedbackToCustomerEmail(string mailTo, string firstName, string feedback)
        {
            var mandrillTemplateId = _options.Value.FeedbackToCustomerId;
            var mandrillKey = _options.Value.MandrillKey;

            var body = new MandrillEmailBodyRequest
            {
                Key = mandrillKey,
                TemplateName = mandrillTemplateId,
                Async = true,
                Message = new Message
                {
                    To = new List<To>
                    {
                        new To
                        {
                            Email = mailTo,
                            Name = firstName,
                            Type = "to"
                        }
                    },
                    GlobalMergeVars = new List<GlobalMergeVar>
                    {
                        new GlobalMergeVar
                        {
                            Name = "FirstName",
                            Content = firstName
                        },
                        new GlobalMergeVar
                        {
                            Name = "Feedback",
                            Content = feedback
                        }
                    },
                    MergeLanguage = Util.GetDescriptionFromEnum(MergeLanguage.HandleBars)
                }
            };

            await SendTemplateMail(body);
        }

        public async Task SendFeedbackToCashrewards(int memberId, string firstName, string lastName, string email, string feedback,
            string appVersion, string deviceModel, string operatingSystem, string buildNumber)
        {
            var mandrillTemplateId = _options.Value.FeedbackToCashrewardsId;
            var mandrillKey = _options.Value.MandrillKey;

            var body = new MandrillEmailBodyRequest
            {
                Key = mandrillKey,
                TemplateName = mandrillTemplateId,
                Async = true,
                Message = new Message
                {
                    To = new List<To>
                    {
                        new To
                        {
                            Email = _options.Value.EmailFeedbackToCashrewards,
                            Name = firstName,
                            Type = "to"
                        }
                    },
                    GlobalMergeVars = new List<GlobalMergeVar>
                    {
                        new GlobalMergeVar
                        {
                            Name = "MemberId",
                            Content = memberId.ToString()
                        },
                        new GlobalMergeVar
                        {
                            Name = "FirstName",
                            Content = firstName
                        },
                        new GlobalMergeVar
                        {
                            Name = "LastName",
                            Content = lastName
                        },
                        new GlobalMergeVar
                        {
                            Name = "Email",
                            Content = email
                        },
                        new GlobalMergeVar
                        {
                            Name = "Feedback",
                            Content = feedback
                        },
                        new GlobalMergeVar
                        {
                            Name = "AppVersion",
                            Content = appVersion
                        },
                        new GlobalMergeVar
                        {
                            Name = "DeviceModel",
                            Content = deviceModel
                        },
                        new GlobalMergeVar
                        {
                            Name = "OperatingSystem",
                            Content = operatingSystem
                        },
                        new GlobalMergeVar
                        {
                            Name = "BuildNumber",
                            Content = buildNumber
                        }
                    },
                    MergeLanguage = Util.GetDescriptionFromEnum(MergeLanguage.HandleBars)
                }
            };

            await SendTemplateMail(body);
        }

        public async Task SendPasswordUpdateEmail(string mailTo, string firstName)
        {
            var mandrillTemplateId = _options.Value.MandrillEmailPasswordUpdateTemplateId;
            var mandrillKey = _options.Value.MandrillKey;

            var body = new MandrillEmailBodyRequest
            {
                Key = mandrillKey,
                TemplateName = mandrillTemplateId,
                TemplateContent = new List<TemplateContent>
                {
                    new TemplateContent
                    {
                        Name = "",
                        Content = ""
                    }
                },
                Async = true,
                Message = new Message
                {
                    To = new List<To>
                    {
                        new To
                        {
                            Email = mailTo,
                            Name = firstName,
                            Type = "to"
                        }
                    },
                    GlobalMergeVars = new List<GlobalMergeVar>
                    {
                        new GlobalMergeVar
                        {
                            Name = "FirstName",
                            Content = firstName
                        }
                    },
                    MergeLanguage = Util.GetDescriptionFromEnum(MergeLanguage.HandleBars)
                }
            };

            await SendTemplateMail(body);
        }

        private async Task SendTemplateMail(MandrillEmailBodyRequest body)
        {
            var apiUrl = new StringBuilder().Append(_options.Value.MandrillApiEndpoint)
                .Append("/messages/send-template.json").ToString();

            var jsonStr = JsonConvert.SerializeObject(body);
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(apiUrl))
            {
                Content = new StringContent(jsonStr, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(httpRequest).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new BadRequestException(await response.Content.ReadAsStringAsync());
            }
        }

        public async Task SendContactMSEmail(string firstName, string mailTo)
        {
            var body = new MandrillEmailBodyRequest
            {
                Key = _options.Value.MandrillKey,
                TemplateName = "cr-contact-ms",
                Async = true,
                Message = new Message
                {
                    To = new List<To>
                    {
                        new To
                        {
                            Email = mailTo,
                            Name = firstName,
                            Type = "to"
                        }
                    },
                    Subject = "Update your mobile number ",
                    GlobalMergeVars = new List<GlobalMergeVar>
                    {
                        new GlobalMergeVar
                        {
                            Name = "FirstName",
                            Content = firstName
                        }
                    },
                    MergeLanguage = Util.GetDescriptionFromEnum(MergeLanguage.HandleBars)
                }
            };
            await SendTemplateMail(body);
        }
    }
}