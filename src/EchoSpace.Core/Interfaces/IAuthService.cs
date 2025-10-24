using EchoSpace.Core.DTOs;

namespace EchoSpace.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<AuthResponse> GoogleLoginAsync(string email, string name, string googleId);
    }
}

