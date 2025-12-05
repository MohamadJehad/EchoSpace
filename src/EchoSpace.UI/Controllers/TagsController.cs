using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.DTOs.Tags;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly ILogger<TagsController> _logger;
        private readonly ITagService _tagService;

        public TagsController(ILogger<TagsController> logger, ITagService tagService)
        {
            _logger = logger;
            _tagService = tagService;
        }

        /// <summary>
        /// Get all tags
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetTags(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all tags");
                var tags = await _tagService.GetAllAsync();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting all tags");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get a specific tag by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TagDto>> GetTag(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var tag = await _tagService.GetByIdAsync(id);
                if (tag == null)
                {
                    return NotFound($"Tag with ID {id} not found");
                }
                return Ok(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting tag {TagId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Create a new tag (Admin only - can be restricted later)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var created = await _tagService.CreateAsync(request);
                return CreatedAtAction(nameof(GetTag), new { id = created.TagId }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating tag");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Initialize default tags (Admin only - can be restricted later)
        /// </summary>
        [HttpPost("initialize")]
        public async Task<ActionResult> InitializeDefaultTags(CancellationToken cancellationToken)
        {
            try
            {
                await _tagService.InitializeDefaultTagsAsync();
                return Ok(new { message = "Default tags initialized successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while initializing default tags");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get trending tags (tags with most posts)
        /// </summary>
        [HttpGet("trending")]
        public async Task<ActionResult<IEnumerable<TrendingTagDto>>> GetTrendingTags([FromQuery] int count = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting trending tags (count: {Count})", count);
                var trendingTags = await _tagService.GetTrendingAsync(count);
                return Ok(trendingTags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting trending tags");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}

