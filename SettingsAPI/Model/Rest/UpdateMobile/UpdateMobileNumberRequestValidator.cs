using FluentValidation;
using SettingsAPI.Common;
using System.Text.RegularExpressions;

namespace SettingsAPI.Model.Rest.UpdateMobile
{
    public class UpdateMobileNumberRequestValidator : AbstractValidator<UpdateMobileNumberRequest>
    {
        public UpdateMobileNumberRequestValidator()
        {
            RuleFor(field => field.MobileNumber).NotEmpty().NotNull().Must(ValidateMobile);
            RuleFor(field => field.MobileOtp).NotEmpty().NotNull();
        }

        private bool ValidateMobile(string mobile)
        {
            if (mobile.StartsWith("(")) return false;
            var regex = new Regex(Constant.AustraliaPhoneRegex);
            if (regex.IsMatch(mobile))
                return true;

            //If false, try match with newzealand phone
            regex = new Regex(Constant.NewzealandPhoneRegex);
            return regex.IsMatch(mobile);
        }
    }
}