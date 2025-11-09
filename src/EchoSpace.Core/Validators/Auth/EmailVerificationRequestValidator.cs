using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class EmailVerificationRequestValidator : AbstractValidator<EmailVerificationRequest>
    {
        public EmailVerificationRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Code)
                .Length(6);
        }
    }
}
