using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class TotpSetupRequestValidator : AbstractValidator<TotpSetupRequest>
    {
        public TotpSetupRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress();

          
        }
    }
}

