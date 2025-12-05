using FluentValidation;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;
using System.Text.RegularExpressions;

namespace EchoSpace.Core.Validators.Posts
{
    public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
    {
        private readonly IGoogleSafeBrowsingService _safeBrowsingService;
        private readonly IGooglePerspectiveService _perspectiveService;

        // Threshold: if toxicity >= ToxicityThreshold => validation fails
        private const double ToxicityThreshold = 0.6;

        public UpdatePostRequestValidator(
            IGoogleSafeBrowsingService safeBrowsingService,
            IGooglePerspectiveService perspectiveService)
        {
            _safeBrowsingService = safeBrowsingService;
            _perspectiveService = perspectiveService;

            RuleFor(x => x.Content)
                .NotEmpty()
                .MaximumLength(5000)
                .Must(BeSafeAndAppropriate)
                .WithMessage("Post contains unsafe or toxic contents.");

            RuleFor(x => x.ImageUrl)
                .MaximumLength(500);
        }

        /// <summary>
        /// Synchronous wrapper that:
        /// - extracts URLs and checks them via SafeBrowsing API (sync-blocking)
        /// - calls Perspective API to obtain toxicity score (sync-blocking)
        /// Returns true if content is acceptable (URLs safe AND toxicity below threshold).
        /// Returns false if any check fails or any API throws an exception.
        /// </summary>
        private bool BeSafeAndAppropriate(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return true; // Not this method's job to fail empty - NotEmpty handles that

            try
            {
                // 1) Check URLs (if any)
               var urlMatches = Regex.Matches(content,@"https?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+");
                Console.WriteLine($"Found {urlMatches.Count} URLs in content.");

                foreach (Match match in urlMatches)
                {
                    // Call async method synchronously
                    Console.WriteLine($"Found URL to check: {match.Value}");
    
                    bool isSafe = _safeBrowsingService.IsUrlSafeAsync(match.Value)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

                    if (!isSafe)
                    {
                        // Unsafe URL -> fail validation
                        return false;
                    }
                }

                // 2) Check toxicity score
                double toxicity = _perspectiveService.GetToxicityScoreAsync(content)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                // Fail if toxicity >= threshold
                if (toxicity >= ToxicityThreshold)
                    return false;

                // All good
                return true;
            }
            catch
            {
                // If the API call fails or any unexpected error occurs, treat as invalid.
                // (Optional: change to 'return true' to allow posts when APIs are down.)
                return false;
            }
        }
    }
}

