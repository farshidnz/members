using FluentValidation;
using SettingsAPI.Common;
using SettingsAPI.Model.Enum;
using SettingsAPI.Service;
using System;
using System.Globalization;

namespace SettingsAPI.Model.Rest
{
    public class MemberDetailsRequestValidator : AbstractValidator<MemberDetailsRequest>
    {
        private readonly IValidationService _validationService;

        public MemberDetailsRequestValidator(IValidationService validationService)
        {
            _validationService = validationService;

            RuleFor(val => val.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(val => val.LastName).NotEmpty().MaximumLength(50);

            RuleFor(val => val.DateOfBirth).Custom((dateOfBirthStr, context) =>
           {
               if (string.IsNullOrEmpty(dateOfBirthStr)) return;
               DateTime dateOfBirth;
               if (!DateTime.TryParseExact(dateOfBirthStr, Constant.DateOfBirthFormat,
                                 CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth))
                   context.AddFailure("Invalid format for DateOfBirth");

               var dobValiation = CheckDateOfBirth(dateOfBirth);
               if (dobValiation.Success == false)
               {
                   context.AddFailure(dobValiation.Message);
               }
           });

            RuleFor(val => val.Gender).Must( description =>
            {
                if (!_validationService.IsEnumDescriptionValid<Gender>(description))
                    return false;

                return true;
            });
            RuleFor(val => val.PostCode).Matches(Constant.PostCodeRegex);
            RuleFor(val => val.MobileOtp).NotEmpty();
            
        }

        private (bool Success, string Message) CheckDateOfBirth(DateTime dateOfBirth)
        {
            var dateNow = DateTime.Today;

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
                    return (false, AppMessage.DateOfBirthNotAllow);

                return (true, null);
            }
            else
                return (false, AppMessage.DateOfBirthWrong);
        }
    }
}