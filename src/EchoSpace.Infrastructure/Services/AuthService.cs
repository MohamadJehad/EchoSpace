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

        public AuthService(EchoSpaceDbContext context, IConfiguration configuration, ILogger<AuthService> logger, IEmailSender emailSender)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailSender = emailSender;
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

            // Generate tokens
            return await GenerateTokensAsync(user);
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

            // Check lockout
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                throw new UnauthorizedAccessException("Account is locked.");
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                // Increment failed attempts
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= 3)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                
                // Generic error to prevent user enumeration
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // Reset failed attempts on successful login
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            return await GenerateTokensAsync(user);
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
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

            // Save reset token to database
            var passwordResetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = resetToken,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(passwordResetToken);
            await _context.SaveChangesAsync();

            // Send reset email
            var resetUrl = $"{_configuration["Frontend:BaseUrl"]}/reset-password?token={Uri.EscapeDataString(resetToken)}";
            var emailBody = $@"
                <h1>Password Reset Request</h1>
                <p>Hello {user.Name},</p>
                <p>You have requested to reset your password for your EchoSpace account.</p>
                <p>Click the link below to reset your password:</p>
                <p><a href=""{resetUrl}"" style=""background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this password reset, please ignore this email.</p>
                <p>Best regards,<br/>The EchoSpace Team</p>
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
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            if (resetToken == null)
            {
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    Message = "Invalid or expired reset token."
                };
            }

            if (resetToken.IsUsed)
            {
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    Message = "This reset token has already been used."
                };
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    Message = "This reset token has expired."
                };
            }

            return new ValidateResetTokenResponse
            {
                IsValid = true,
                Message = "Reset token is valid."
            };
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
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

        private string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}

