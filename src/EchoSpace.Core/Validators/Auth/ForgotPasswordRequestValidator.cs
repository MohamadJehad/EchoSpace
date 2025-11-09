using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
    {
        public ForgotPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

        }
    }
}
