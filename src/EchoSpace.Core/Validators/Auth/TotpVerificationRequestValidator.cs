
using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class TotpVerificationRequestValidator : AbstractValidator<TotpVerificationRequest>
    {
        public TotpVerificationRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress();

            RuleFor(x => x.Code).NotEmpty().Length(6);
        }
    }
}
