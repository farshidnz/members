using FluentValidation;

namespace SettingsAPI.Model.Rest.VerifyEmail
{
    public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
    {
        public VerifyEmailRequestValidator()
        {
            RuleFor(field => field.Code).NotEmpty().NotNull();
        }
    }
}