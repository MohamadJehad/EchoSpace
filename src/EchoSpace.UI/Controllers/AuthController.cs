using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Interfaces.Services;
using EchoSpace.Tools.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITotpService _totpService;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEmailSender _emailSender;
        private readonly EchoSpaceDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, ITotpService totpService, ILogger<AuthController> logger, IHttpClientFactory httpClientFactory, IEmailSender emailSender, EchoSpaceDbContext context, IAuditLogService auditLogService, IConfiguration configuration)
        {
            _authService = authService;
            _totpService = totpService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _emailSender = emailSender;
            _context = context;
            _auditLogService = auditLogService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                
                // Audit log successful registration
                await _auditLogService.LogAsync(
                    action: "Register",
                    entityType: "User",
                    entityId: response.User?.Id.ToString() ?? request.Email,
                    result: "Success",
                    newValues: new Dictionary<string, object> { { "Email", request.Email }, { "Name", request.Name ?? "N/A" } }
                );
                
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed for email: {Email}", request.Email);
                
                // Audit log failed registration attempt
                await _auditLogService.LogAsync(
                    action: "Register",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Failed",
                    newValues: new Dictionary<string, object> { { "Email", request.Email }, { "Reason", ex.Message } }
                );
                
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                
                // Audit log registration error
                await _auditLogService.LogAsync(
                    action: "Register",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Error",
                    newValues: new Dictionary<string, object> { { "Email", request.Email } }
                );
                
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                
                // Audit log successful login
                await _auditLogService.LogAsync(
                    action: "Login",
                    entityType: "User",
                    entityId: response.User?.Id.ToString() ?? request.Email,
                    result: "Success",
                    newValues: new Dictionary<string, object> { { "Email", request.Email } }
                );
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Get user to check lockout status and failed attempts
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                var maxAttempts = int.TryParse(_configuration["AccountLockout:MaxFailedAttempts"], out var max) ? max : 5;
                
                var isLocked = user != null && user.LockoutEnabled && 
                               user.LockoutEnd.HasValue && 
                               user.LockoutEnd.Value > DateTimeOffset.UtcNow;
                
                var failedAttempts = user?.AccessFailedCount ?? 0;
                var remainingAttempts = Math.Max(0, maxAttempts - failedAttempts);
                
                // Audit log failed login attempt
                await _auditLogService.LogAsync(
                    action: "Login",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Failed",
                    newValues: new Dictionary<string, object> 
                    { 
                        { "Email", request.Email }, 
                        { "Reason", ex.Message },
                        { "FailedAttempts", failedAttempts },
                        { "IsLocked", isLocked }
                    }
                );
                
                // Return detailed error information for frontend
                if (isLocked)
                {
                    return Unauthorized(new 
                    { 
                        message = ex.Message,
                        isLocked = true,
                        failedAttempts = failedAttempts,
                        remainingAttempts = 0
                    });
                }
                
                // Return failed attempts info if account is not locked yet
                return Unauthorized(new 
                { 
                    message = "Invalid credentials.",
                    isLocked = false,
                    failedAttempts = failedAttempts,
                    remainingAttempts = remainingAttempts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                
                // Audit log login error
                await _auditLogService.LogAsync(
                    action: "Login",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Error",
                    newValues: new Dictionary<string, object> { { "Email", request.Email } }
                );
                
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request.RefreshToken);
                
                // Audit log successful token refresh
                await _auditLogService.LogAsync(
                    action: "RefreshToken",
                    entityType: "UserSession",
                    entityId: "TokenRefresh",
                    result: "Success"
                );
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Audit log failed token refresh
                await _auditLogService.LogAsync(
                    action: "RefreshToken",
                    entityType: "UserSession",
                    entityId: "TokenRefresh",
                    result: "Failed",
                    newValues: new Dictionary<string, object> { { "Reason", ex.Message } }
                );
                
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                
                // Audit log token refresh error
                await _auditLogService.LogAsync(
                    action: "RefreshToken",
                    entityType: "UserSession",
                    entityId: "TokenRefresh",
                    result: "Error"
                );
                
                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                await _authService.LogoutAsync(request.RefreshToken);
                
                // Audit log logout
                await _auditLogService.LogAsync(
                    action: "Logout",
                    entityType: "UserSession",
                    entityId: "SessionTerminated",
                    result: "Success"
                );
                
                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                
                // Audit log logout error
                await _auditLogService.LogAsync(
                    action: "Logout",
                    entityType: "UserSession",
                    entityId: "SessionTerminated",
                    result: "Error"
                );
                
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        [HttpGet("google")]
        public async Task<IActionResult> GoogleLogin()
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var clientId = configuration["Google:ClientId"];
            var redirectUri = configuration["OAuth:CallbackUrl"];
            var state = Guid.NewGuid().ToString(); // CSRF protection
            
            // Ensure session is loaded and store state
            await HttpContext.Session.LoadAsync();
            HttpContext.Session.SetString("oauth_state", state);
            await HttpContext.Session.CommitAsync(); // Ensure session is persisted
            
            _logger.LogInformation("OAuth state stored. State: {State}, SessionId: {SessionId}", state, HttpContext.Session.Id);
            
            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id={Uri.EscapeDataString(clientId!)}&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri!)}&" +
                $"response_type=code&" +
                $"scope=openid email profile&" +
                $"state={state}";
            
            return Redirect(googleAuthUrl);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var clientId = configuration["Google:ClientId"];
                var clientSecret = configuration["Google:ClientSecret"];
                var redirectUri = configuration["OAuth:CallbackUrl"];
                var frontendCallbackUrl = configuration["OAuth:FrontendCallbackUrl"];

                // Verify state (CSRF protection)
                // Ensure session is loaded
                await HttpContext.Session.LoadAsync();
                
                var storedState = HttpContext.Session.GetString("oauth_state");
                if (string.IsNullOrEmpty(storedState) || storedState != state)
                {
                    _logger.LogWarning("OAuth state validation failed. Stored: {StoredState}, Received: {ReceivedState}, SessionId: {SessionId}", 
                        storedState ?? "null", state, HttpContext.Session.Id);
                    
                    // Audit log failed OAuth attempt
                    await _auditLogService.LogAsync(
                        action: "GoogleOAuthLogin",
                        entityType: "User",
                        entityId: "Unknown",
                        result: "Failed - Invalid State",
                        newValues: new Dictionary<string, object> { { "Reason", "Invalid state parameter" } }
                    );
                    
                    return BadRequest(new { message = "Invalid state parameter. Session may have expired. Please try again." });
                }

                // Exchange authorization code for access token
                var httpClient = _httpClientFactory.CreateClient();
                var tokenRequest = new Dictionary<string, string>
                {
                    { "code", code },
                    { "client_id", clientId! },
                    { "client_secret", clientSecret! },
                    { "redirect_uri", redirectUri! },
                    { "grant_type", "authorization_code" }
                };

                var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token",
                    new FormUrlEncodedContent(tokenRequest));

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Failed to exchange authorization code." });
                }

                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
                var accessToken = tokenData.GetProperty("access_token").GetString();

                // Get user info from Google
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var userInfoResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                
                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Failed to retrieve user information from Google." });
                }

                var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoContent);
                
                var email = userInfo.GetProperty("email").GetString();
                var name = userInfo.GetProperty("name").GetString();
                var googleId = userInfo.GetProperty("id").GetString();

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(googleId))
                {
                    string message = "Failed to retrieve user information from Google.";
                    return BadRequest(new { message });
                }

                var authResponse = await _authService.GoogleLoginAsync(email, name, googleId);

                // Audit log successful Google OAuth login
                await _auditLogService.LogAsync(
                    action: "GoogleOAuthLogin",
                    entityType: "User",
                    entityId: authResponse.User?.Id.ToString() ?? email,
                    result: "Success",
                    newValues: new Dictionary<string, object> { { "Email", email }, { "Name", name }, { "GoogleId", googleId } }
                );

                // URL encode the tokens
                var encodedAccessToken = Uri.EscapeDataString(authResponse.AccessToken);
                var encodedRefreshToken = Uri.EscapeDataString(authResponse.RefreshToken);
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var encodedUser = Uri.EscapeDataString(JsonSerializer.Serialize(authResponse.User, jsonOptions));

                // Redirect to Angular with tokens in URL
                var redirectUrl = $"{frontendCallbackUrl}?accessToken={encodedAccessToken}&refreshToken={encodedRefreshToken}&user={encodedUser}";
                return Redirect(redirectUrl);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Google OAuth login blocked: {Message}", ex.Message);
                
                // Audit log blocked Google OAuth login (likely due to lockout)
                await _auditLogService.LogAsync(
                    action: "GoogleOAuthLogin",
                    entityType: "User",
                    entityId: "Unknown",
                    result: "Blocked - Account Locked",
                    newValues: new Dictionary<string, object> { { "Reason", ex.Message } }
                );
                
                // Redirect to frontend with error message
                var frontendCallbackUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["OAuth:FrontendCallbackUrl"];
                var encodedError = Uri.EscapeDataString(ex.Message);
                var redirectUrl = $"{frontendCallbackUrl}?error={encodedError}";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication");
                
                // Audit log Google OAuth login failure
                await _auditLogService.LogAsync(
                    action: "GoogleOAuthLogin",
                    entityType: "User",
                    entityId: "Unknown",
                    result: "Error"
                );
                
                return StatusCode(500, new { message = "An error occurred during Google authentication." });
            }
        }

        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                var emailBody = $@"
                    <h1>Welcome to EchoSpace!</h1>
                    <p>This is a test email from EchoSpace.</p>
                    <p>If you received this email, the email service is working correctly.</p>
                    <p>Best regards,<br/>The EchoSpace Team</p>
                ";

                await _emailSender.SendEmailAsync(request.Email, "EchoSpace Email Test", emailBody);
                
                _logger.LogInformation("Test email sent successfully to {Email}", request.Email);
                return Ok(new { message = "Test email sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", request.Email);
                return StatusCode(500, new { message = "Failed to send test email." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var response = await _authService.ForgotPasswordAsync(request);
                
                // Audit log password reset request
                await _auditLogService.LogAsync(
                    action: "ForgotPassword",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Success"
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request for email: {Email}", request.Email);
                
                // Audit log password reset request failure
                await _auditLogService.LogAsync(
                    action: "ForgotPassword",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Error"
                );
                
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken([FromBody] ValidateResetTokenRequest request)
        {
            try
            {
                // URL decode the token to handle + characters that get converted to spaces in URLs
                var decodedToken = Uri.UnescapeDataString(request.Token);
                _logger.LogInformation("Original token: {OriginalToken}", request.Token);
                _logger.LogInformation("Decoded token: {DecodedToken}", decodedToken);
                
                // Create a new request with the decoded token
                var decodedRequest = new ValidateResetTokenRequest { Token = decodedToken };
                var response = await _authService.ValidateResetTokenAsync(decodedRequest);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token");
                return StatusCode(500, new { message = "An error occurred while validating the reset token." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                // URL decode the token to handle + characters that get converted to spaces in URLs
                var decodedToken = Uri.UnescapeDataString(request.Token);
                _logger.LogInformation("Original token: {OriginalToken}", request.Token);
                _logger.LogInformation("Decoded token: {DecodedToken}", decodedToken);
                
                // Create a new request with the decoded token
                var decodedRequest = new ResetPasswordRequest 
                { 
                    Token = decodedToken, 
                    NewPassword = request.NewPassword 
                };
                
                var success = await _authService.ResetPasswordAsync(decodedRequest);
                
                if (success)
                {
                    // Audit log successful password reset (CRITICAL SECURITY EVENT)
                    await _auditLogService.LogAsync(
                        action: "ResetPassword",
                        entityType: "User",
                        entityId: "PasswordChanged",
                        result: "Success",
                        newValues: new Dictionary<string, object> { { "TokenUsed", "Yes" } }
                    );
                    
                    return Ok(new { message = "Password has been reset successfully." });
                }
                else
                {
                    // Audit log failed password reset attempt
                    await _auditLogService.LogAsync(
                        action: "ResetPassword",
                        entityType: "User",
                        entityId: "PasswordChangeAttempt",
                        result: "Failed",
                        newValues: new Dictionary<string, object> { { "Reason", "Invalid or expired token" } }
                    );
                    
                    return BadRequest(new { message = "Invalid or expired reset token." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new { message = "An error occurred while resetting the password." });
            }
        }

        [HttpGet("debug-reset-tokens")]
        public async Task<IActionResult> DebugResetTokens()
        {
            try
            {
                // This is a debug endpoint to help troubleshoot token issues
                // Remove this in production
                var tokens = await _context.PasswordResetTokens
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .Select(t => new
                    {
                        t.Id,
                        t.Token,
                        t.UserId,
                        t.ExpiresAt,
                        t.IsUsed,
                        t.CreatedAt,
                        UserEmail = t.User.Email
                    })
                    .ToListAsync();

                return Ok(new { 
                    message = "Debug endpoint - remove in production",
                    tokens = tokens
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving debug tokens");
                return StatusCode(500, new { message = "An error occurred while retrieving debug tokens." });
            }
        }

        [HttpPost("setup-totp")]
        public async Task<IActionResult> SetupTotp([FromBody] TotpSetupRequest request)
        {
            try
            {
                var response = await _totpService.SetupTotpAsync(request.Email);
                
                // Audit log TOTP setup
                await _auditLogService.LogAsync(
                    action: "SetupTOTP",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Success"
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up TOTP for {Email}", request.Email);
                
                // Audit log TOTP setup failure
                await _auditLogService.LogAsync(
                    action: "SetupTOTP",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Error"
                );
                
                return StatusCode(500, new { message = "An error occurred setting up TOTP." });
            }
        }

        [HttpPost("verify-totp")]
        public async Task<IActionResult> VerifyTotp([FromBody] TotpVerificationRequest request)
        {
            try
            {
                var response = await _authService.VerifyTotpAndLoginAsync(request.Email, request.Code);
                
                // Audit log successful TOTP verification/login
                await _auditLogService.LogAsync(
                    action: "VerifyTOTP",
                    entityType: "User",
                    entityId: response.User?.Id.ToString() ?? request.Email,
                    result: "Success",
                    newValues: new Dictionary<string, object> { { "Email", request.Email } }
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying TOTP for {Email}", request.Email);
                
                // Audit log failed TOTP verification
                await _auditLogService.LogAsync(
                    action: "VerifyTOTP",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Failed",
                    newValues: new Dictionary<string, object> { { "Email", request.Email } }
                );
                
                return StatusCode(500, new { message = "An error occurred verifying TOTP." });
            }
        }

        [HttpPost("send-email-verification")]
        public async Task<IActionResult> SendEmailVerification([FromBody] TotpSetupRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { message = "Email is required." });
                }

                _logger.LogInformation("Received request to send email verification to {Email}", request.Email);
                
                var success = await _totpService.SendEmailVerificationCodeAsync(request.Email);
                if (success)
                {
                    return Ok(new { message = "Verification code sent to your email." });
                }
                return BadRequest(new { message = "User not found or failed to send verification code. Please ensure the email is registered." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification to {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred sending verification code." });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
        {
            try
            {
                var isValid = await _totpService.VerifyEmailCodeAsync(request.Email, request.Code);
                if (isValid)
                {
                    // Audit log successful email verification
                    await _auditLogService.LogAsync(
                        action: "VerifyEmail",
                        entityType: "User",
                        entityId: request.Email,
                        result: "Success"
                    );
                    
                    return Ok(new { message = "Email verified successfully." });
                }
                
                // Audit log failed email verification
                await _auditLogService.LogAsync(
                    action: "VerifyEmail",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Failed",
                    newValues: new Dictionary<string, object> { { "Reason", "Invalid verification code" } }
                );
                
                return BadRequest(new { message = "Invalid verification code." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", request.Email);
                
                // Audit log email verification error
                await _auditLogService.LogAsync(
                    action: "VerifyEmail",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Error"
                );
                
                return StatusCode(500, new { message = "An error occurred verifying email." });
            }
        }

        [HttpPost("setup-totp-for-existing-user")]
        public async Task<IActionResult> SetupTotpForExistingUser([FromBody] TotpSetupRequest request)
        {
            try
            {
                // Check if user exists
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                // Allow reconfiguration - we'll generate a new TOTP secret even if one exists

                var response = await _totpService.SetupTotpAsync(request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up TOTP for existing user {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred setting up TOTP." });
            }
        }

        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
        {
            try
            {
                var response = await _authService.CompleteRegistrationWithEmailVerificationAsync(request.Email, request.Code);
                
                // Audit log completed registration
                await _auditLogService.LogAsync(
                    action: "CompleteRegistration",
                    entityType: "User",
                    entityId: response.User?.Id.ToString() ?? request.Email,
                    result: "Success",
                    newValues: new Dictionary<string, object> { { "Email", request.Email } }
                );
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Audit log failed registration completion
                await _auditLogService.LogAsync(
                    action: "CompleteRegistration",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Failed",
                    newValues: new Dictionary<string, object> { { "Email", request.Email }, { "Reason", ex.Message } }
                );
                
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing registration for {Email}", request.Email);
                
                // Audit log registration completion error
                await _auditLogService.LogAsync(
                    action: "CompleteRegistration",
                    entityType: "User",
                    entityId: request.Email,
                    result: "Error"
                );
                
                return StatusCode(500, new { message = "An error occurred completing registration." });
            }
        }

        /// <summary>
        /// Unlock account using token from email
        /// </summary>
        [HttpPost("unlock-account")]
        public async Task<IActionResult> UnlockAccount([FromBody] UnlockAccountRequest request)
        {
            try
            {
                var success = await _authService.UnlockAccountAsync(request.Token);
                if (success)
                {
                    return Ok(new { message = "Account unlocked successfully. You can now log in." });
                }
                return BadRequest(new { message = "Invalid or expired unlock token." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account with token");
                return StatusCode(500, new { message = "An error occurred while unlocking the account." });
            }
        }

        /// <summary>
        /// Request unlock email (for locked accounts)
        /// </summary>
        [HttpPost("request-unlock-email")]
        public async Task<IActionResult> RequestUnlockEmail([FromBody] RequestUnlockEmailRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    // Don't reveal if user exists
                    return Ok(new { message = "If the account exists and is locked, an unlock email has been sent." });
                }

                await _authService.SendUnlockEmailAsync(user);
                return Ok(new { message = "If the account exists and is locked, an unlock email has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending unlock email to {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while sending the unlock email." });
            }
        }
    }

    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class CompleteRegistrationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class UnlockAccountRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class RequestUnlockEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}


