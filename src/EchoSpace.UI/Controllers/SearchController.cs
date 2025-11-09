using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.DTOs;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Search for users with fuzzy matching (70% similarity threshold)
        /// </summary>
        /// <param name="q">Search query</param>
        /// <param name="limit">Maximum number of results (default: 10)</param>
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<SearchResultDto>>> SearchUsers(
            [FromQuery] string q,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("Search query cannot be empty");
                }

                if (limit < 1 || limit > 50)
                {
                    return BadRequest("Limit must be between 1 and 50");
                }

                _logger.LogInformation("Searching users with query: {Query}", q);

                var results = await _searchService.SearchUsersAsync(q, limit);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query: {Query}", q);
                return StatusCode(500, "An error occurred while searching");
            }
        }
    }
}

