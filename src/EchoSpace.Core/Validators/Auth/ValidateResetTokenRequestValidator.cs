using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class ValidateResetTokenRequestValidator : AbstractValidator<ValidateResetTokenRequest>
    {
        public ValidateResetTokenRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty();

          
        }
    }
}

