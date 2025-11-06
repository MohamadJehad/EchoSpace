using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Tools.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace EchoSpace.Infrastructure.Services
{
    public class TotpService : ITotpService
    {
        private readonly EchoSpaceDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TotpService> _logger;
        private readonly IEmailSender _emailSender;

        public TotpService(
            EchoSpaceDbContext context, 
            IConfiguration configuration, 
            ILogger<TotpService> logger,
            IEmailSender emailSender)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task<TotpSetupResponse> SetupTotpAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Generate secret key (20 bytes = 160 bits)
            var secretKey = GenerateSecretKey();
            var base32Key = ToBase32(secretKey);

            // Store secret key
            user.TotpSecretKey = base32Key;
            await _context.SaveChangesAsync();

            // Generate QR code
            var qrCodeUrl = await GenerateQrCodeAsync(base32Key, email);

            return new TotpSetupResponse
            {
                QrCodeUrl = qrCodeUrl,
                SecretKey = base32Key,
                ManualEntryKey = base32Key
            };
        }

        public async Task<bool> VerifyTotpAsync(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || string.IsNullOrEmpty(user.TotpSecretKey))
                return false;

            var secretKey = FromBase32(user.TotpSecretKey);
            var currentTimeStep = GetCurrentTimeStep();
            
            // Check current and previous/next time steps for clock drift tolerance
            for (int i = -1; i <= 1; i++)
            {
                var timeStep = currentTimeStep + i;
                var expectedCode = GenerateTotpCode(secretKey, timeStep);
                if (expectedCode == code)
                {
                    // TOTP is now mandatory, no need to set a flag
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> SendEmailVerificationCodeAsync(string email)
        {
            try
            {
                _logger.LogInformation("Attempting to send email verification code to {Email}", email);
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", email);
                    return false;
                }

                // Check if there's already a valid (non-expired) code
                if (!string.IsNullOrEmpty(user.EmailVerificationCode) && 
                    user.EmailVerificationCodeExpiry.HasValue && 
                    user.EmailVerificationCodeExpiry.Value > DateTime.UtcNow)
                {
                    _logger.LogInformation("Email verification code already exists and is still valid for {Email}. Skipping new code generation.", email);
                    return true;
                }

                // Generate 6-digit code
                var code = new Random().Next(100000, 999999).ToString();
                var expiry = DateTime.UtcNow.AddMinutes(10);

                // Store code
                user.EmailVerificationCode = code;
                user.EmailVerificationCodeExpiry = expiry;
                user.EmailVerificationAttempts = 0;
                await _context.SaveChangesAsync();

                // Send email using your existing SMTP service
                var subject = "EchoSpace - Email Verification Code";
                var htmlContent = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background-color: #f8f9fa; padding: 30px; border-radius: 10px; text-align: center;'>
                            <h2 style='color: #333; margin-bottom: 20px;'>Email Verification</h2>
                            <p style='font-size: 16px; color: #666; margin-bottom: 20px;'>
                                Your verification code is:
                            </p>
                            <div style='background-color: #007bff; color: white; font-size: 24px; font-weight: bold; padding: 15px; border-radius: 5px; margin: 20px 0; letter-spacing: 3px;'>
                                {code}
                            </div>
                            <p style='font-size: 14px; color: #999; margin-top: 20px;'>
                                This code will expire in 10 minutes.
                            </p>
                            <p style='font-size: 12px; color: #ccc; margin-top: 30px;'>
                                If you didn't request this code, please ignore this email.
                            </p>
                        </div>
                    </body>
                    </html>";

                await _emailSender.SendEmailAsync(email, subject, htmlContent);
                _logger.LogInformation("Email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email verification code to {Email}", email);
                return false;
            }
        }

        public async Task<bool> VerifyEmailCodeAsync(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || string.IsNullOrEmpty(user.EmailVerificationCode))
                return false;

            // Check if code has expired
            if (user.EmailVerificationCodeExpiry < DateTime.UtcNow)
                return false;

            // Check attempt limit
            if (user.EmailVerificationAttempts >= 3)
                return false;

            // Verify code
            if (user.EmailVerificationCode == code)
            {
                user.EmailVerified = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationCodeExpiry = null;
                user.EmailVerificationAttempts = 0;
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                user.EmailVerificationAttempts++;
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public Task<string> GenerateQrCodeAsync(string secretKey, string email)
        {
            var issuer = "EchoSpace";
            var accountTitle = email;
            var manualEntryKey = secretKey;

            var qrCodeText = $"otpauth://totp/{issuer}:{accountTitle}?secret={manualEntryKey}&issuer={issuer}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeText, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            var qrCodeBytes = qrCode.GetGraphic(20);
            return Task.FromResult(Convert.ToBase64String(qrCodeBytes));
        }

        private byte[] GenerateSecretKey()
        {
            var key = new byte[20]; // 160 bits
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return key;
        }

        private string ToBase32(byte[] data)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            var bits = 0;
            var value = 0;

            foreach (var b in data)
            {
                value = (value << 8) | b;
                bits += 8;

                while (bits >= 5)
                {
                    result.Append(base32Chars[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                result.Append(base32Chars[(value << (5 - bits)) & 31]);
            }

            return result.ToString();
        }

        private byte[] FromBase32(string base32)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new List<byte>();
            var bits = 0;
            var value = 0;

            foreach (var c in base32.ToUpper())
            {
                var index = base32Chars.IndexOf(c);
                if (index == -1) continue;

                value = (value << 5) | index;
                bits += 5;

                if (bits >= 8)
                {
                    result.Add((byte)((value >> (bits - 8)) & 255));
                    bits -= 8;
                }
            }

            return result.ToArray();
        }

        private long GetCurrentTimeStep()
        {
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return unixTime / 30; // 30-second time steps
        }

        private string GenerateTotpCode(byte[] secretKey, long timeStep)
        {
            var timeStepBytes = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timeStepBytes);
            }

            using var hmac = new HMACSHA1(secretKey);
            var hash = hmac.ComputeHash(timeStepBytes);
            var offset = hash[hash.Length - 1] & 0x0F;
            var code = ((hash[offset] & 0x7F) << 24) |
                      ((hash[offset + 1] & 0xFF) << 16) |
                      ((hash[offset + 2] & 0xFF) << 8) |
                      (hash[offset + 3] & 0xFF);

            return (code % 1000000).ToString("D6");
        }
    }
}
