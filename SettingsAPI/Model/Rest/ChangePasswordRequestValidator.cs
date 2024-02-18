using FluentValidation;
using SettingsAPI.Common;

namespace SettingsAPI.Model.Rest
{
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(val => val.MobileOtp).NotEmpty();
            RuleFor(val => val.NewPassword).NotEmpty().MinimumLength(Constant.PasswordMinLength);
        }
    }
}