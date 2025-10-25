using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;

        public UsersController(ILogger<UsersController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        /// <summary>
        /// Get suggested users for the current user
        /// </summary>
        [HttpGet("suggested")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetSuggestedUsers([FromQuery] int count = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting suggested users (count: {Count})", count);
                var users = await _userService.GetAllAsync();
                
                // Transform to suggested user format
                var suggestedUsers = users
                    .Where(u => u.Role == Core.Enums.UserRole.User) // Only regular users
                    .OrderByDescending(u => u.CreatedAt) // Most recent first
                    .Take(count)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        username = u.UserName,
                        email = u.Email,
                        createdAt = u.CreatedAt,
                        postsCount = u.Posts?.Count ?? 0
                    })
                    .ToList();

                return Ok(suggestedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting suggested users");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all users");
                var users = await _userService.GetAllAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting all users");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var created = await _userService.CreateAsync(request);
                return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating user {UserName}", request?.Name);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var user = await _userService.UpdateAsync(id, request);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating user {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _userService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"User with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting user {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
