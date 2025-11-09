using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Interfaces;
using EchoSpace.Tools.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(
        IAuthService authService,
        ITotpService totpService,
        ILogger<AuthController> logger,
        IHttpClientFactory httpClientFactory,
        IEmailSender emailSender,
        EchoSpaceDbContext context) : ControllerBase
    {
        [HttpPost("register")]
        [EnableRateLimiting("LoginAndRegisterPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Registration failed for email: {Email}", request.Email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginAndRegisterPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Get user to check failed attempts count
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
                // Check if it's a lockout message
                if (ex.Message.Contains("locked"))
                {
                    return Unauthorized(new { 
                        message = ex.Message,
                        isLocked = true,
                        failedAttempts = user?.AccessFailedCount ?? 0,
                        maxAttempts = 5
                    });
                }
            
                // Return failed attempts count for invalid credentials
                return Unauthorized(new { 
                    message = "Invalid credentials.",
                    failedAttempts = user?.AccessFailedCount ?? 0,
                    maxAttempts = 5,
                    remainingAttempts = user != null ? Math.Max(0, 5 - user.AccessFailedCount) : 5
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("RefreshTokenPolicy")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                await authService.LogoutAsync(request.RefreshToken);
                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during logout");
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

                // Exchange authorization code for an access token
                var httpClient = httpClientFactory.CreateClient();
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

                var authResponse = await authService.GoogleLoginAsync(email, name, googleId);

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
                logger.LogError(ex, "Error during Google authentication");
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

                await emailSender.SendEmailAsync(request.Email, "EchoSpace Email Test", emailBody);
            
                logger.LogInformation("Test email sent successfully to {Email}", request.Email);
                return Ok(new { message = "Test email sent successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send test email to {Email}", request.Email);
                return StatusCode(500, new { message = "Failed to send test email." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var response = await authService.ForgotPasswordAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during forgot password request for email: {Email}", request.Email);
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
                logger.LogInformation("Original token: {OriginalToken}", request.Token);
                logger.LogInformation("Decoded token: {DecodedToken}", decodedToken);
            
                // Create a new request with the decoded token
                var decodedRequest = new ValidateResetTokenRequest { Token = decodedToken };
                var response = await authService.ValidateResetTokenAsync(decodedRequest);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating reset token");
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
                logger.LogInformation("Original token: {OriginalToken}", request.Token);
                logger.LogInformation("Decoded token: {DecodedToken}", decodedToken);
            
                // Create a new request with the decoded token
                var decodedRequest = new ResetPasswordRequest 
                { 
                    Token = decodedToken, 
                    NewPassword = request.NewPassword 
                };
            
                var success = await authService.ResetPasswordAsync(decodedRequest);
            
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
                logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new { message = "An error occurred while resetting the password." });
            }
        }

        [HttpPost("verify-totp")]
        public async Task<IActionResult> VerifyTotp([FromBody] TotpVerificationRequest request)
        {
            try
            {
                var response = await authService.VerifyTotpAndLoginAsync(request.Email, request.Code);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying TOTP for {Email}", request.Email);
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

                logger.LogInformation("Received request to send email verification to {Email}", request.Email);
            
                var success = await totpService.SendEmailVerificationCodeAsync(request.Email);
                if (success)
                {
                    return Ok(new { message = "Verification code sent to your email." });
                }
                return BadRequest(new { message = "User not found or failed to send verification code. Please ensure the email is registered." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending email verification to {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred sending verification code." });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
        {
            try
            {
                var isValid = await totpService.VerifyEmailCodeAsync(request.Email, request.Code);
                if (isValid)
                {
                    return Ok(new { message = "Email verified successfully." });
                }
                return BadRequest(new { message = "Invalid verification code." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying email for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred verifying email." });
            }
        }

        [HttpPost("setup-totp-for-existing-user")]
        public async Task<IActionResult> SetupTotpForExistingUser([FromBody] TotpSetupRequest request)
        {
            try
            {
                // Check if a user exists
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                // Allow reconfiguration - we'll generate a new TOTP secret even if one exists

                var response = await totpService.SetupTotpAsync(request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting up TOTP for existing user {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred setting up TOTP." });
            }
        }

        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
        {
            try
            {
                var response = await authService.CompleteRegistrationWithEmailVerificationAsync(request.Email, request.Code);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error completing registration for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred completing registration." });
            }
        }

        [HttpPost("unlock-account")]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> UnlockAccount([FromBody] UnlockAccountRequest request)
        {
            try
            {
                // URL decode the token to handle + characters that get converted to spaces in URLs
                var decodedToken = Uri.UnescapeDataString(request.Token);
            
                var success = await authService.UnlockAccountAsync(decodedToken);
                if (success)
                {
                    return Ok(new { message = "Account unlocked successfully. You can now log in." });
                }
                return BadRequest(new { message = "Invalid or expired unlock token." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error unlocking account");
                return StatusCode(500, new { message = "An error occurred while unlocking the account." });
            }
        }

        [HttpPost("request-unlock")]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> RequestUnlock([FromBody] RequestUnlockRequest request)
        {
            try
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    // Generic response to prevent user enumeration
                    return Ok(new { message = "If an account exists with this email, an unlock link has been sent." });
                }
            
                // Check if an account is actually locked
                if (!user.LockoutEnabled || !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow)
                {
                    return Ok(new { message = "Your account is not locked." });
                }
            
                await authService.SendUnlockEmailAsync(user);
                return Ok(new { message = "If an account exists with this email, an unlock link has been sent." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error requesting unlock for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        // Admin unlock endpoint (requires admin role)
        [HttpPost("admin/unlock-account")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminUnlockAccount([FromBody] AdminUnlockRequest request)
        {
            try
            {
                var success = await authService.AdminUnlockAccountAsync(request.UserId);
                if (success)
                {
                    return Ok(new { message = "Account unlocked successfully." });
                }
                return BadRequest(new { message = "User not found or account is not locked." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error unlocking account {UserId} by admin", request.UserId);
                return StatusCode(500, new { message = "An error occurred while unlocking the account." });
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

    public class RequestUnlockRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class AdminUnlockRequest
    {
        public Guid UserId { get; set; }
    }
}
