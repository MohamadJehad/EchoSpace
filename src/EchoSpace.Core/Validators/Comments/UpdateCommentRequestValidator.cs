
using FluentValidation;
using EchoSpace.Core.DTOs.Comments;


namespace EchoSpace.Core.Validators.Comments
{
    public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
    {
        public UpdateCommentRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().MaximumLength(2000);


        }
    }
}
