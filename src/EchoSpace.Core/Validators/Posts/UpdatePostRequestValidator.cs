
using FluentValidation;
using EchoSpace.Core.DTOs.Posts;


namespace EchoSpace.Core.Validators.Posts
{
    public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
    {
        public UpdatePostRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().MaximumLength(5000);

            RuleFor(x => x.ImageUrl)
                .MaximumLength(500);

        }
    }
}

