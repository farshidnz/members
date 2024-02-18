using FluentValidation;

namespace SettingsAPI.Model.Rest.UpdateEmail
{
    public class UpdateEmailRequestValidator : AbstractValidator<EmailUpdateRequest>
    {
        public UpdateEmailRequestValidator()
        {
            RuleFor(field => field.Email).NotEmpty().NotNull().EmailAddress();
            RuleFor(field => field.MobileOtp).NotEmpty().NotNull();
        }
    }
}