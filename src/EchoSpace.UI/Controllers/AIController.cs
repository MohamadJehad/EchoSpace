using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;
using EchoSpace.Tools.Sanitizer;

namespace SocialApp.Api.Controllers
{
    [ApiController] 
    [Route("api/posts")]
    public class PostAiController : ControllerBase
    {
        private readonly IPostRepository _repo;
        private readonly IAiService _ai;
        private readonly ILogger<PostAiController> _logger;

        public PostAiController(
            IPostRepository repo,
            IAiService ai,
            ILogger<PostAiController> logger)
        {
            _repo = repo;
            _ai = ai;
            _logger = logger;
        }

        [HttpPost("translate")]
        public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
        {
            _logger.LogInformation("Translate request for PostId {PostId} to {Lang}", 
                request.PostId, request.Language);

            var post = await _repo.GetByIdAsync(request.PostId);

            if (post == null)
            {
                _logger.LogWarning("PostId {PostId} not found", request.PostId);
                return NotFound("Post not found");
            }

            var cleaned = PromptSanitizer.Clean(post.Content);
            var translated = await _ai.TranslateTextAsync(cleaned, request.Language);

            return Ok(new
            {
                request.PostId,
                request.Language,
                translated
            });
        }

        [HttpPost("summarize")]
        public async Task<IActionResult> Summarize([FromBody] SummarizeRequest request)
        {
            _logger.LogInformation("Summarize request for PostId {PostId}",
                request.PostId);

            var post = await _repo.GetByIdAsync(request.PostId);

            if (post == null)
            {
                _logger.LogWarning("PostId {PostId} not found", request.PostId);
                return NotFound("Post not found");
            }

            var cleaned = PromptSanitizer.Clean(post.Content);
            var summary = await _ai.SummarizeTextAsync(cleaned);

            return Ok(new
            {
                request.PostId,
                summary
            });
        }
    }
}
