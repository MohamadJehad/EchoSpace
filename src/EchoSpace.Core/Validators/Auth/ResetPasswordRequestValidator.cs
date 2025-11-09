using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty();

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(10)
                .Matches(@"^(?=.*[A-Z])(?=.*[@$!%*?&#^~])[A-Za-z\d@$!%*?&#^~]{10,}$")
                .WithMessage("Password must be at least 10 characters with at least one uppercase letter and one special character (@$!%*?&#^~).");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword);
        }
    }
}

