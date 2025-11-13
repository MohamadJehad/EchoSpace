using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.Interfaces.Services;
using EchoSpace.Core.DTOs.Dashboard;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IAnalyticsService analyticsService,
            ILogger<DashboardController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard overview with all metrics
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetOverview()
        {
            try
            {
                var overview = await _analyticsService.GetDashboardOverviewAsync();
                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard overview");
                return StatusCode(500, new { message = "An error occurred while retrieving dashboard data." });
            }
        }

        /// <summary>
        /// Get user growth time series data
        /// </summary>
        [HttpGet("users/growth")]
        public async Task<ActionResult<TimeSeriesDataDto>> GetUserGrowth([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                {
                    return BadRequest(new { message = "Days must be between 1 and 365." });
                }

                var data = await _analyticsService.GetUserGrowthAsync(days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user growth data");
                return StatusCode(500, new { message = "An error occurred while retrieving user growth data." });
            }
        }

        /// <summary>
        /// Get post activity time series data
        /// </summary>
        [HttpGet("posts/activity")]
        public async Task<ActionResult<TimeSeriesDataDto>> GetPostActivity([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                {
                    return BadRequest(new { message = "Days must be between 1 and 365." });
                }

                var data = await _analyticsService.GetPostActivityAsync(days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post activity data");
                return StatusCode(500, new { message = "An error occurred while retrieving post activity data." });
            }
        }

        /// <summary>
        /// Get login activity time series data
        /// </summary>
        [HttpGet("login/activity")]
        public async Task<ActionResult<TimeSeriesDataDto>> GetLoginActivity([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                {
                    return BadRequest(new { message = "Days must be between 1 and 365." });
                }

                var data = await _analyticsService.GetLoginActivityAsync(days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting login activity data");
                return StatusCode(500, new { message = "An error occurred while retrieving login activity data." });
            }
        }

        /// <summary>
        /// Get all active sessions
        /// </summary>
        [HttpGet("sessions/active")]
        public async Task<ActionResult<List<ActiveSessionDto>>> GetActiveSessions()
        {
            try
            {
                var sessions = await _analyticsService.GetActiveSessionsAsync();
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions");
                return StatusCode(500, new { message = "An error occurred while retrieving active sessions." });
            }
        }

        /// <summary>
        /// Get sessions for a specific user
        /// </summary>
        [HttpGet("sessions/user/{userId}")]
        public async Task<ActionResult<List<ActiveSessionDto>>> GetUserSessions(Guid userId)
        {
            try
            {
                var sessions = await _analyticsService.GetUserSessionsAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sessions for {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving user sessions." });
            }
        }

        /// <summary>
        /// Get failed login attempts report
        /// </summary>
        [HttpGet("security/failed-attempts")]
        public async Task<ActionResult<List<FailedLoginAttemptDto>>> GetFailedLoginAttempts([FromQuery] int top = 50)
        {
            try
            {
                if (top < 1 || top > 500)
                {
                    return BadRequest(new { message = "Top must be between 1 and 500." });
                }

                var attempts = await _analyticsService.GetFailedLoginAttemptsAsync(top);
                return Ok(attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed login attempts");
                return StatusCode(500, new { message = "An error occurred while retrieving failed login attempts." });
            }
        }

        /// <summary>
        /// Terminate a specific session
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<ActionResult> TerminateSession(Guid sessionId)
        {
            try
            {
                var success = await _analyticsService.TerminateSessionAsync(sessionId);
                if (!success)
                {
                    return NotFound(new { message = "Session not found." });
                }

                return Ok(new { message = "Session terminated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
                return StatusCode(500, new { message = "An error occurred while terminating the session." });
            }
        }

        /// <summary>
        /// Terminate all sessions for a user
        /// </summary>
        [HttpDelete("sessions/user/{userId}")]
        public async Task<ActionResult> TerminateUserSessions(Guid userId)
        {
            try
            {
                var count = await _analyticsService.TerminateUserSessionsAsync(userId);
                return Ok(new { message = $"Terminated {count} session(s) successfully.", count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating sessions for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while terminating user sessions." });
            }
        }
    }
}

