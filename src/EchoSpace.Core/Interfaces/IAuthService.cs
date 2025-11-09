using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Entities;

namespace EchoSpace.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> VerifyTotpAndLoginAsync(string email, string totpCode);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<AuthResponse> GoogleLoginAsync(string email, string name, string googleId);
        
        // Password reset methods
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ValidateResetTokenResponse> ValidateResetTokenAsync(ValidateResetTokenRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        
        // Email verification methods
        Task<AuthResponse> CompleteRegistrationWithEmailVerificationAsync(string email, string verificationCode);
        
        // Account unlock methods
        Task<bool> UnlockAccountAsync(string token);
        Task SendUnlockEmailAsync(User user);
        Task<bool> AdminUnlockAccountAsync(Guid userId);
    }
}

