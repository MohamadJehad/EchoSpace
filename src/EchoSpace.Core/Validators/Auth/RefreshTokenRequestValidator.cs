using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty();

          
        }
    }
}


