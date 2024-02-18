using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SettingsAPI.Common;
using SettingsAPI.Error;
using SettingsAPI.Model.Enum;

namespace SettingsAPI.Service
{
    public class ValidationService : IValidationService
    {
        private readonly IMobileOptService _mobileOptService;
        private readonly IAwsService _awsService;
        private readonly IOptions<Settings> _options;
        private readonly ITimeService _timeService;

        public ValidationService(IMobileOptService mobileOptService, IAwsService awsService,
            IOptions<Settings> options, ITimeService timeService)
        {
            _mobileOptService = mobileOptService;
            _awsService = awsService;
            _options = options;
            _timeService = timeService;
        }

        public void ValidateQueryConditions(int limit, int offset, string dateFromStr, string dateToStr,
            string orderBy, string sortDirection, ApiUsed apiUsed)
        {
            if (limit <= 0)
                throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.Limit));

            if (offset < 0)
                throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.Offset));

            DateTime? dateFrom = null;
            if (dateFromStr != null)
            {
                try
                {
                    dateFrom = DateTime.ParseExact(dateFromStr, Constant.DateQueryParameterFormat,
                        CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom));
                }
            }

            DateTime? dateTo = null;
            if (dateToStr != null)
            {
                try
                {
                    dateTo = DateTime.ParseExact(dateToStr, Constant.DateQueryParameterFormat,
                        CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.DateTo));
                }
            }

            if (dateFrom != null && dateTo != null && (dateFrom > dateTo))
            {
                throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom));
            }

            switch (apiUsed)
            {
                case ApiUsed.Transaction:
                {
                    if (!Enum.TryParse(orderBy, out TransactionOrderByField _))
                        throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.OrderBy));
                    break;
                }
                case ApiUsed.MemberClickHistory:
                {
                    if (!Enum.TryParse(orderBy, out MemberClickHistoryOrderByField _))
                        throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.OrderBy));
                    break;
                }
                default:
                    throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.OrderBy));
            }

            if (!Enum.TryParse(sortDirection, out SortDirection _))
                throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.SorDirection));
        }

        public void ValidateQueryConditionsForTotalCount(string dateFromStr, string dateToStr)
        {
            DateTime? dateFrom = null;
            if (dateFromStr != null)
            {
                try
                {
                    dateFrom = DateTime.ParseExact(dateFromStr, Constant.DateQueryParameterFormat,
                        CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new InvalidQueryConditionException(
                        Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom));
                }
            }

            DateTime? dateTo = null;
            if (dateToStr != null)
            {
                try
                {
                    dateTo = DateTime.ParseExact(dateToStr, Constant.DateQueryParameterFormat,
                        CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.DateTo));
                }
            }

            if (dateFrom != null && dateTo != null && (dateFrom > dateTo))
            {
                throw new InvalidQueryConditionException(Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom));
            }
        }

        public void ValidateEmail(string email)
        {
            var isValidate = Util.IsValidEmail(email);
            if (!isValidate || email.Length > Constant.EmailMaxLength)
                throw new InvalidEmailException();
        }

        public bool IsEnumDescriptionValid<T>(string description)
        {
            var descriptions = new DescriptionAttributes<T>().Descriptions.ToList();
            return descriptions.Contains(description);
        }

        public void ValidateGender(string gender)
        {
            var descriptions = new DescriptionAttributes<Gender>().Descriptions.ToList();
            if (!descriptions.Contains(gender))
                throw new InvalidGenderException();
        }

        public DateTime ValidateAndParseDateOfBirth(string dateOfBirthStr)
        {
            try
            {
                var dateOfBirth = DateTime.ParseExact(dateOfBirthStr, Constant.DateOfBirthFormat,
                    CultureInfo.InvariantCulture);
                //Check date of birth must early than and greater than 14 ages from current date
                CheckDateOfBirth(dateOfBirth);

                return dateOfBirth;
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(InvalidDateOfBirthException))
                    throw;

                throw new InvalidDateOfBirthException(string.Format(AppMessage.FieldInvalid, "Date of birth"));
            }
        }

        public void ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.StartsWith("("))
                throw new InvalidMobileNumberException();

            //Try match with australia phone
            var regex = new Regex(Constant.AustraliaPhoneRegex);

            if (regex.IsMatch(phone)) return;

            //If false, try match with newzealand phone
            regex = new Regex(Constant.NewzealandPhoneRegex);
            if (!regex.IsMatch(phone))
                throw new InvalidMobileNumberException();
        }

        public void ValidateOtp(string phone, string mobileOtp, string email)
        {         
            if (!_mobileOptService.VerifyMobileOtp(phone, mobileOtp, email))
                throw new InvalidMobileOtpException();
        }


        public void ValidateAccountNumber(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length < 6 || accountNumber.Length > 10)
                throw new BankAccountValidationException(
                    Util.GetDescriptionFromEnum(BankAccountValidationFields.AccountNumber));

            if (!Regex.IsMatch(accountNumber, Constant.AccountNumberRegex))
                throw new BankAccountValidationException(Util.GetDescriptionFromEnum(BankAccountValidationFields.AccountNumber));
        }

        public async Task ValidateBsb(string bsb)
        {
            var bsbList = await _awsService.FetchBsbData();
            var enumerable = bsbList as string[] ?? bsbList.ToArray();

            if (!enumerable.Contains(bsb))
                throw new BankAccountValidationException(Util.GetDescriptionFromEnum(BankAccountValidationFields.Bsb));
        }

        public void ValidateAccountName(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName) || (!Regex.IsMatch(accountName, Constant.AccountNameRegex)))
                throw new BankAccountValidationException(
                    Util.GetDescriptionFromEnum(BankAccountValidationFields.AccountName));
        }

        public void ValidatePostCode(string postCode)
        {
            if (string.IsNullOrWhiteSpace(postCode) || (!Regex.IsMatch(postCode, Constant.PostCodeRegex)))
                throw new InvalidPostCodeException();
        }
        
        public void ValidateAmount(decimal amount, decimal balance)
        {
            if (amount < 0)
                throw new InvalidAmountException(string.Format(AppMessage.FieldInvalid, "Amount"));

            if (amount < Convert.ToDecimal(_options.Value.MinRedemptionAmount))
                throw new InvalidAmountException(string.Format(AppMessage.MinimumAmountRequired,
                    _options.Value.MinRedemptionAmount));
            
            if (Math.Round(balance, 2) < amount)
                throw new InvalidAmountException(AppMessage.AmountGreaterThanAvailableRewards);
            
            if (amount >= Convert.ToInt32(_options.Value.MaxRedemptionAmount))
                throw new InvalidAmountException(AppMessage.AmountGreaterThanMaximumLimit);
        }

        public void ValidatePaymentMethod(string paymentMethod)
        {
            if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethod))
                throw new InvalidPaymentMethodException();
        }

        public void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < Constant.PasswordMinLength)
            {
                throw new InvalidPasswordException("Your password");
            }
        }

        public void ValidateUri(string uri)
        {
            if (!Regex.IsMatch(uri, Constant.UriRegex))
            {
                 throw new InvalidUriException();
            }
        }

        public void ValidateEmailVerificationCode(string code, out string memberIdStr, out string hashedEmail)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidEmailVerificationCodeException();
            
            string[] decodeDataArr;
            try
            {
                decodeDataArr = code.Split("$");
            }
            catch
            {
                throw new InvalidEmailVerificationCodeException();
            }

            if (decodeDataArr.Length < 2)
                throw new InvalidEmailVerificationCodeException();

            memberIdStr = decodeDataArr[0];
            hashedEmail = string.Join("$", decodeDataArr.Skip(1).ToArray());

            if (string.IsNullOrWhiteSpace(memberIdStr) || string.IsNullOrWhiteSpace(hashedEmail))
                throw new InvalidEmailVerificationCodeException();
        }

        public void ValidateCheckMobileLinkCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BadRequestException("Bad paramers");

            if (!Regex.IsMatch(code, Constant.MobileLinkCode))
            {
                throw new BadRequestException("Bad paramers");
            }    
        }

        public void ValidateName(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName) || (firstName.Length <= 1) || (firstName.Length > 50))
                throw new InvalidNameException("First name");

            if (string.IsNullOrWhiteSpace(lastName) || (lastName.Length <= 1) || (lastName.Length > 50))
                throw new InvalidNameException("Last name");
        }

        public void ValidateFeedback(string feedback)
        {
            if (string.IsNullOrWhiteSpace(feedback))
                throw new InvalidFeedbackException();
        }

        public void ValidateAppVersion(string appVersion)
        {
            if (string.IsNullOrWhiteSpace(appVersion))
                throw new InvalidAppVersionException();
        }

        public void ValidateDeviceModel(string deviceModel)
        {
            if (string.IsNullOrWhiteSpace(deviceModel))
                throw new InvalidDeviceModelException();
        }

        public void ValidateOperatingSystem(string operatingSystem)
        {
            if (string.IsNullOrWhiteSpace(operatingSystem))
                throw new InvalidOperationException();
        }

        public void ValidateBuildNumber(string buildNumber)
        {
            if (string.IsNullOrWhiteSpace(buildNumber))
                throw new InvalidBuildNumberException();
        }

        private class DescriptionAttributes<T>
        {
            private List<DescriptionAttribute> Attributes = new List<DescriptionAttribute>();
            internal List<string> Descriptions { get; set; }
 
            public DescriptionAttributes()
            {
                RetrieveAttributes();
                Descriptions = Attributes.Select(x => x.Description).ToList();
            }

            private void RetrieveAttributes()
            {
                foreach (var attribute in typeof(T).GetMembers().SelectMany(member =>
                    member.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>()))
                    Attributes.Add(attribute);
            }
        }

        private void CheckDateOfBirth(DateTime dateOfBirth)
        {
            var dateNow = _timeService.GetCurrentDateTimeToday();

            if ((dateNow.Year - dateOfBirth.Year) > 0 ||
                (((dateNow.Year - dateOfBirth.Year) == 0) && ((dateOfBirth.Month < dateNow.Month) ||
                                                              ((dateOfBirth.Month == dateNow.Month) &&
                                                               (dateOfBirth.Day <= dateNow.Day)))))
            {
                int age;
                if (dateNow.Month > dateOfBirth.Month)
                    age = dateNow.Year - dateOfBirth.Year;
                else if (dateNow.Month == dateOfBirth.Month)
                {
                    if (dateNow.Day >= dateOfBirth.Day)
                        age = dateNow.Year - dateOfBirth.Year;
                    else
                        age = (dateNow.Year - 1) - dateOfBirth.Year;
                }
                else
                    age = (dateNow.Year - 1) - dateOfBirth.Year;

                if (age < Constant.MinUserAgeAllow)
                    throw new InvalidDateOfBirthException(AppMessage.DateOfBirthNotAllow);
            }
            else
                throw new InvalidDateOfBirthException(AppMessage.DateOfBirthWrong);
        }
    }
}