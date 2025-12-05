
using FluentValidation;
using EchoSpace.Core.DTOs;


namespace EchoSpace.Core.Validators.Users
{
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            // Name is optional, but if provided, must be valid
            When(x => !string.IsNullOrEmpty(x.Name), () => {
                RuleFor(x => x.Name)
                    .MaximumLength(100);
            });

            // Email is optional, but if provided, must be valid
            When(x => !string.IsNullOrEmpty(x.Email), () => {
                RuleFor(x => x.Email)
                    .EmailAddress();
            });
        }
    }
}


