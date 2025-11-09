using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
    {
        public LogoutRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty();

          
        }
    }
}
