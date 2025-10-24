using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public AuthService(EchoSpaceDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
                    Email = user.Email
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
    }
}

