using EchoSpace.Core.DTOs.Auth;

namespace EchoSpace.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<AuthResponse> GoogleLoginAsync(string email, string name, string googleId);
        
        // Password reset methods
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ValidateResetTokenResponse> ValidateResetTokenAsync(ValidateResetTokenRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    }
}

