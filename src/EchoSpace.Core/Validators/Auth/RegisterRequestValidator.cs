using FluentValidation;
using EchoSpace.Core.DTOs.Auth;


namespace EchoSpace.Core.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(10)
                .Matches(@"^(?=.*[A-Z])(?=.*[@$!%*?&#^~])[A-Za-z\d@$!%*?&#^~]{10,}$")
                .WithMessage("Password must be at least 10 characters with at least one uppercase letter and one special character (@$!%*?&#^~).");
        }
    }
}

