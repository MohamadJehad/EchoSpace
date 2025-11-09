
using FluentValidation;
using EchoSpace.Core.DTOs.Comments;


namespace EchoSpace.Core.Validators.Comments
{
    public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
    {
        public CreateCommentRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().MaximumLength(2000);


        }
    }
}

