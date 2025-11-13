using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Tools.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EchoSpace.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly EchoSpaceDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ITotpService _totpService;

        public AuthService(EchoSpaceDbContext context, IConfiguration configuration, ILogger<AuthService> logger, IEmailSender emailSender, ITotpService totpService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailSender = emailSender;
            _totpService = totpService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                Name = request.Name,
                EmailConfirmed = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            // Hash password
            user.PasswordHash = HashPassword(request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send email verification code
            await _totpService.SendEmailVerificationCodeAsync(user.Email);

            // Don't generate tokens yet - user needs to verify email first
            // Return a response indicating email verification is required
            return new AuthResponse
            {
                RequiresEmailVerification = true,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    UserName = user.UserName,
                    Role = user.Role.ToString()
                }
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Generic error to prevent user enumeration
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // Check if account is locked
            if (IsAccountLocked(user))
            {
                var lockoutInfo = GetLockoutInfo(user);
                throw new UnauthorizedAccessException($"Account is locked. {lockoutInfo}");
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                await HandleFailedLoginAttempt(user);
                
                // Generic error to prevent user enumeration
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // Reset failed attempts on successful login
            await ResetLockoutAsync(user);
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Check if user has TOTP secret key set up
            if (string.IsNullOrEmpty(user.TotpSecretKey))
            {
                // User needs to set up TOTP first
                return new AuthResponse
                {
                    RequiresTotp = true,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        UserName = user.UserName,
                        Role = user.Role.ToString()
                    }
                };
            }

            // User has TOTP set up, return response indicating TOTP verification is required
            return new AuthResponse
            {
                RequiresTotp = true,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    UserName = user.UserName,
                    Role = user.Role.ToString()
                }
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

            if (session == null || session.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            // Update session expiry
            session.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            // Generate new tokens
            return await GenerateTokensAsync(session.User);
        }

        public async Task<AuthResponse> VerifyTotpAndLoginAsync(string email, string totpCode)
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            // Check if TOTP is set up
            if (string.IsNullOrEmpty(user.TotpSecretKey))
            {
                throw new UnauthorizedAccessException("Please set up TOTP first by going to the registration flow.");
            }

            // Verify TOTP code using the TotpService
            var isValidTotp = await _totpService.VerifyTotpAsync(email, totpCode);
            
            if (!isValidTotp)
            {
                throw new UnauthorizedAccessException("Invalid TOTP code.");
            }

            // Generate tokens for successful login
            return await GenerateTokensAsync(user);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
            
            if (session != null)
            {
                _context.UserSessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<AuthResponse> GoogleLoginAsync(string email, string name, string googleId)
        {
            // Find existing user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Check if existing user account is locked (new users won't be locked)
            if (user != null && IsAccountLocked(user))
            {
                var lockoutInfo = GetLockoutInfo(user);
                throw new UnauthorizedAccessException($"Account is locked. {lockoutInfo}");
            }

            if (user == null)
            {
                // Create new user for Google authentication
                user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    Name = name,
                    EmailConfirmed = true, // Google verifies emails
                    LockoutEnabled = false,
                    AccessFailedCount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create AuthProvider entry
                var authProvider = new AuthProvider
                {
                    AuthId = Guid.NewGuid(),
                    UserId = user.Id,
                    Provider = "Google",
                    ProviderUid = googleId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuthProviders.Add(authProvider);
                await _context.SaveChangesAsync();
            }
            else
            {
                // User exists, check if Google auth is linked
                var existingProvider = await _context.AuthProviders
                    .FirstOrDefaultAsync(ap => ap.UserId == user.Id && ap.Provider == "Google");

                if (existingProvider == null)
                {
                    // Link Google account to existing user
                    var authProvider = new AuthProvider
                    {
                        AuthId = Guid.NewGuid(),
                        UserId = user.Id,
                        Provider = "Google",
                        ProviderUid = googleId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.AuthProviders.Add(authProvider);
                    await _context.SaveChangesAsync();
                }
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            return await GenerateTokensAsync(user);
        }

        private async Task<AuthResponse> GenerateTokensAsync(User user)
        {
            // Invalidate all existing active sessions for this user to prevent multiple active sessions
            var existingSessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            
            if (existingSessions.Any())
            {
                _context.UserSessions.RemoveRange(existingSessions);
            }

            // Generate access token
            var accessToken = GenerateAccessToken(user);

            // Generate refresh token
            var refreshToken = GenerateRefreshToken();

            // Save refresh token to database
            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = user.Id,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "15") * 60,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    UserName = user.UserName,
                    Role = user.Role.ToString()
                }
            };
        }

        private string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // Add JWT standard claims for user ID
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim("user_id", user.Id.ToString()),
                new Claim("userId", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "15")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return $"{Convert.ToBase64String(salt)}:{hashed}";
        }

        private bool VerifyPassword(string password, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = parts[1];

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hash == hashed;
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            // Always return success to prevent user enumeration
            if (user == null)
            {
                return new ForgotPasswordResponse
                {
                    Message = "If an account with that email exists, password reset instructions have been sent.",
                    Success = true
                };
            }

            // Invalidate any existing reset tokens for this user
            var existingTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToListAsync();
            
            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            // Generate secure reset token
            var resetToken = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

            _logger.LogInformation("Generated reset token: {ResetToken} for user {UserId}", resetToken, user.Id);
            _logger.LogInformation("Token expires at: {ExpiresAt}", expiresAt);

            // Save reset token to database
            var passwordResetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = resetToken,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Generated password reset token for user {UserId}", user.Id);
            _logger.LogInformation("Password reset token details: Id={Id}, UserId={UserId}, Token={Token}, ExpiresAt={ExpiresAt}", 
                passwordResetToken.Id, passwordResetToken.UserId, passwordResetToken.Token, passwordResetToken.ExpiresAt);
            
            _context.PasswordResetTokens.Add(passwordResetToken);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Password reset token saved to database successfully");

            // Send reset email
            var resetUrl = $"{_configuration["Frontend:BaseUrl"]}/reset-password?token={resetToken}";
            var emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Password Reset - EchoSpace</title>
                </head>
                <body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
                    <div style=""background-color: #f8f9fa; padding: 30px; border-radius: 10px; border: 1px solid #e9ecef;"">
                        <h1 style=""color: #007bff; text-align: center; margin-bottom: 30px;"">Password Reset Request</h1>
                        
                        <p>Hello {user.Name},</p>
                        
                        <p>You have requested to reset your password for your EchoSpace account.</p>
                        
                        <p>Click the button below to reset your password:</p>
                        
                        <div style=""text-align: center; margin: 30px 0;"">
                            <a href=""{resetUrl}"" style=""background-color: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold; font-size: 16px;"">Reset Password</a>
                        </div>
                        
                        <p style=""font-size: 15px; color: #666;"">Or copy the following link.</p>
                        <p style=""font-size: 15px; color: #666;"">{resetUrl}</p>
                        
                        <p style=""font-size: 14px; color: #666;"">This link will expire in 1 hour for security reasons.</p>
                        
                        <p style=""font-size: 14px; color: #666;"">If you didn't request this password reset, please ignore this email. Your account remains secure.</p>
                        
                        <hr style=""border: none; border-top: 1px solid #e9ecef; margin: 30px 0;"">
                        
                        <p style=""font-size: 12px; color: #999; text-align: center;"">
                            Best regards,<br/>
                            The EchoSpace Team<br/>
                            <a href=""{_configuration["Frontend:BaseUrl"]}"" style=""color: #007bff;"">EchoSpace</a>
                        </p>
                    </div>
                </body>
                </html>
            ";

            try
            {
                await _emailSender.SendEmailAsync(user.Email, "Password Reset Request - EchoSpace", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // Don't throw exception to maintain security (don't reveal if email sending failed)
            }

            return new ForgotPasswordResponse
            {
                Message = "If an account with that email exists, password reset instructions have been sent.",
                Success = true
            };
        }

        public async Task<ValidateResetTokenResponse> ValidateResetTokenAsync(ValidateResetTokenRequest request)
        {
            _logger.LogInformation("Validating reset token: {Token}", request.Token);
            
            // First, let's check if there are any tokens in the database at all
            var allTokens = await _context.PasswordResetTokens.ToListAsync();
            _logger.LogInformation("Total reset tokens in database: {Count}", allTokens.Count);
            
            foreach (var token in allTokens)
            {
                _logger.LogInformation("Token in DB: {Token}, ExpiresAt: {ExpiresAt}, IsUsed: {IsUsed}, UserId: {UserId}", 
                    token.Token, token.ExpiresAt, token.IsUsed, token.UserId);
            }

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            _logger.LogInformation("Found token in database: {Found}", resetToken != null);

            if (resetToken == null)
            {
                _logger.LogWarning("Reset token not found in database: {Token}", request.Token);
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    Message = "Invalid or expired reset token."
                };
            }

            _logger.LogInformation("Token details - ExpiresAt: {ExpiresAt}, IsUsed: {IsUsed}, CurrentTime: {CurrentTime}", 
                resetToken.ExpiresAt, resetToken.IsUsed, DateTime.UtcNow);

            if (resetToken.IsUsed)
            {
                _logger.LogWarning("Reset token has already been used: {Token}", request.Token);
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    Message = "This reset token has already been used."
                };
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Reset token has expired. ExpiresAt: {ExpiresAt}, CurrentTime: {CurrentTime}", 
                    resetToken.ExpiresAt, DateTime.UtcNow);
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    Message = "This reset token has expired."
                };
            }

            _logger.LogInformation("Reset token is valid: {Token}", request.Token);
            var response = new ValidateResetTokenResponse
            {
                IsValid = true,
                Message = "Reset token is valid."
            };
            
            _logger.LogInformation("Returning validation response: IsValid={IsValid}, Message={Message}", 
                response.IsValid, response.Message);
            
            return response;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            _logger.LogInformation("Attempting to reset password with token: {Token}", request.Token);
            
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token);
            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            // Update user password
            resetToken.User.PasswordHash = HashPassword(request.NewPassword);
            resetToken.User.UpdatedAt = DateTime.UtcNow;

            // Mark token as used
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;

            // Invalidate all user sessions (force re-login)
            var userSessions = await _context.UserSessions
                .Where(s => s.UserId == resetToken.UserId)
                .ToListAsync();
            
            _context.UserSessions.RemoveRange(userSessions);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<AuthResponse> CompleteRegistrationWithEmailVerificationAsync(string email, string verificationCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            // Verify email code using TotpService
            var isValid = await _totpService.VerifyEmailCodeAsync(email, verificationCode);
            
            if (!isValid)
            {
                throw new UnauthorizedAccessException("Invalid verification code.");
            }

            // Mark email as confirmed
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Now generate and return tokens
            return await GenerateTokensAsync(user);
        }

        private string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        // Account Lockout Methods
        private bool IsAccountLocked(User user)
        {
            if (!user.LockoutEnabled) return false;

            // Check if lockout period has expired
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return true;
            }

            // Auto-unlock if lockout expired
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTimeOffset.UtcNow)
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
                _context.SaveChangesAsync(); // Fire and forget
                return false;
            }

            return false;
        }

        private async Task HandleFailedLoginAttempt(User user)
        {
            user.AccessFailedCount++;
            var maxAttempts = int.TryParse(_configuration["AccountLockout:MaxFailedAttempts"], out var max) ? max : 5;
            var lockoutDuration = int.TryParse(_configuration["AccountLockout:LockoutDurationMinutes"], out var duration) ? duration : 30;

            if (user.AccessFailedCount >= maxAttempts)
            {
                // Lock the account after max failed attempts
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(lockoutDuration);
                user.UpdatedAt = DateTime.UtcNow;

                // Send lockout notification email
                var enableEmailUnlock = bool.TryParse(_configuration["AccountLockout:EnableEmailUnlock"], out var enable) ? enable : true;
                if (enableEmailUnlock)
                {
                    await SendLockoutNotificationAsync(user);
                }

                _logger.LogWarning("Account locked for user {UserId} after {Attempts} failed attempts. Lockout ends at {LockoutEnd}", 
                    user.Id, user.AccessFailedCount, user.LockoutEnd);
            }

            await _context.SaveChangesAsync();
        }

        private string GetLockoutInfo(User user)
        {
            if (!user.LockoutEnd.HasValue) return string.Empty;

            var remainingTime = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
            if (remainingTime.TotalMinutes > 0)
            {
                return $"Please try again in {Math.Ceiling(remainingTime.TotalMinutes)} minutes, or use the unlock link sent to your email.";
            }

            return "Your account has been unlocked. Please try logging in again.";
        }

        private async Task ResetLockoutAsync(User user)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
        }

        private async Task SendLockoutNotificationAsync(User user)
        {
            try
            {
                var emailBody = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset=""utf-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Account Locked - EchoSpace</title>
                    </head>
                    <body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
                        <div style=""background-color: #fff3cd; padding: 30px; border-radius: 10px; border: 1px solid #ffc107;"">
                            <h1 style=""color: #856404; text-align: center; margin-bottom: 30px;"">Account Locked</h1>
                            
                            <p>Hello {user.Name},</p>
                            
                            <p>Your account has been temporarily locked due to multiple failed login attempts.</p>
                            
                            <div style=""background-color: #f8f9fa; padding: 20px; border-radius: 5px; border-left: 4px solid #856404; margin: 20px 0;"">
                                <p style=""margin: 0; font-size: 15px; color: #333;"">
                                    <strong>To unlock your account, please contact our support team.</strong>
                                </p>
                            </div>
                            
                            <p style=""font-size: 14px; color: #666;"">
                                Our support team will be able to assist you in unlocking your account and ensuring the security of your information.
                            </p>
                            
                            <p style=""font-size: 14px; color: #666;"">
                                <strong>If you didn't attempt to log in, please contact support immediately to secure your account.</strong>
                            </p>
                            
                            <hr style=""border: none; border-top: 1px solid #e9ecef; margin: 30px 0;"">
                            
                            <p style=""font-size: 12px; color: #999; text-align: center;"">
                                Best regards,<br/>
                                The EchoSpace Team<br/>
                                <a href=""{_configuration["Frontend:BaseUrl"]}"" style=""color: #007bff;"">EchoSpace</a>
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailSender.SendEmailAsync(user.Email, "Account Locked - EchoSpace", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send lockout notification email to {Email}", user.Email);
                // Don't throw - lockout should still proceed even if email fails
            }
        }

        public async Task<bool> UnlockAccountAsync(string token)
        {
            var unlockToken = await _context.AccountUnlockTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (unlockToken == null || unlockToken.IsUsed || unlockToken.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            // Unlock account
            unlockToken.User.AccessFailedCount = 0;
            unlockToken.User.LockoutEnd = null;

            // Mark token as used
            unlockToken.IsUsed = true;
            unlockToken.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Account unlocked for user {UserId} using token", unlockToken.UserId);
            return true;
        }

        public async Task SendUnlockEmailAsync(User user)
        {
            if (!IsAccountLocked(user)) return;

            try
            {
                var unlockToken = await GenerateUnlockTokenAsync(user);
                var unlockUrl = $"{_configuration["Frontend:BaseUrl"]}/unlock-account?token={unlockToken}";

                var emailBody = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset=""utf-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Unlock Your Account - EchoSpace</title>
                    </head>
                    <body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
                        <div style=""background-color: #f8f9fa; padding: 30px; border-radius: 10px; border: 1px solid #e9ecef;"">
                            <h1 style=""color: #007bff; text-align: center; margin-bottom: 30px;"">Unlock Your Account</h1>
                            
                            <p>Hello {user.Name},</p>
                            
                            <p>You have requested to unlock your EchoSpace account.</p>
                            
                            <p>Click the button below to unlock your account:</p>
                            
                            <div style=""text-align: center; margin: 30px 0;"">
                                <a href=""{unlockUrl}"" style=""background-color: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold; font-size: 16px;"">Unlock My Account</a>
                            </div>
                            
                            <p style=""font-size: 15px; color: #666;"">Or copy the following link:</p>
                            <p style=""font-size: 15px; color: #666;"">{unlockUrl}</p>
                            
                            <p style=""font-size: 14px; color: #666;"">This link will expire in 1 hour for security reasons.</p>
                            
                            <hr style=""border: none; border-top: 1px solid #e9ecef; margin: 30px 0;"">
                            
                            <p style=""font-size: 12px; color: #999; text-align: center;"">
                                Best regards,<br/>
                                The EchoSpace Team<br/>
                                <a href=""{_configuration["Frontend:BaseUrl"]}"" style=""color: #007bff;"">EchoSpace</a>
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailSender.SendEmailAsync(user.Email, "Unlock Your Account - EchoSpace", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send unlock email to {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> AdminUnlockAccountAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Fully unlock the account and reset failed attempts
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Account unlocked for user {UserId} by admin. Failed attempts reset to 0.", userId);
            return true;
        }

        private async Task<string> GenerateUnlockTokenAsync(User user)
        {
            // Invalidate any existing unlock tokens for this user
            var existingTokens = await _context.AccountUnlockTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            // Generate secure unlock token
            var unlockToken = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

            // Save unlock token to database
            var accountUnlockToken = new AccountUnlockToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = unlockToken,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.AccountUnlockTokens.Add(accountUnlockToken);
            await _context.SaveChangesAsync();

            return unlockToken;
        }
    }
}

