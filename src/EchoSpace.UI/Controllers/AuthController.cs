using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Interfaces;
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

        public AuthController(IAuthService authService, ITotpService totpService, ILogger<AuthController> logger, IHttpClientFactory httpClientFactory, IEmailSender emailSender, EchoSpaceDbContext context)
        {
            _authService = authService;
            _totpService = totpService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed for email: {Email}", request.Email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                // Generic error to prevent user enumeration
                return Unauthorized(new { message = "Invalid credentials." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                await _authService.LogoutAsync(request.RefreshToken);
                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var clientId = configuration["Google:ClientId"];
            var redirectUri = configuration["OAuth:CallbackUrl"];
            var state = Guid.NewGuid().ToString(); // CSRF protection
            
            // Store state in session or use a more secure method
            HttpContext.Session.SetString("oauth_state", state);
            
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
                var storedState = HttpContext.Session.GetString("oauth_state");
                if (string.IsNullOrEmpty(storedState) || storedState != state)
                {
                    return BadRequest(new { message = "Invalid state parameter." });
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

                // URL encode the tokens
                var encodedAccessToken = Uri.EscapeDataString(authResponse.AccessToken);
                var encodedRefreshToken = Uri.EscapeDataString(authResponse.RefreshToken);
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var encodedUser = Uri.EscapeDataString(JsonSerializer.Serialize(authResponse.User, jsonOptions));

                // Redirect to Angular with tokens in URL
                var redirectUrl = $"{frontendCallbackUrl}?accessToken={encodedAccessToken}&refreshToken={encodedRefreshToken}&user={encodedUser}";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication");
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request for email: {Email}", request.Email);
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
                    return Ok(new { message = "Password has been reset successfully." });
                }
                else
                {
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up TOTP for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred setting up TOTP." });
            }
        }

        [HttpPost("verify-totp")]
        public async Task<IActionResult> VerifyTotp([FromBody] TotpVerificationRequest request)
        {
            try
            {
                var response = await _authService.VerifyTotpAndLoginAsync(request.Email, request.Code);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying TOTP for {Email}", request.Email);
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
                    return Ok(new { message = "Email verified successfully." });
                }
                return BadRequest(new { message = "Invalid verification code." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", request.Email);
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
    }

    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}


