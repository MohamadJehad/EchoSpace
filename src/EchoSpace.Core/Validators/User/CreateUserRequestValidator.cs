
using FluentValidation;
using EchoSpace.Core.DTOs;


namespace EchoSpace.Core.Validators.Users
{
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress();

        }
    }
}


