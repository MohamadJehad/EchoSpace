using EchoSpace.Core.DTOs.Auth;

namespace EchoSpace.Core.Interfaces
{
    public interface ITotpService
    {
        Task<TotpSetupResponse> SetupTotpAsync(string email);
        Task<bool> VerifyTotpAsync(string email, string code);
        Task<bool> SendEmailVerificationCodeAsync(string email);
        Task<bool> VerifyEmailCodeAsync(string email, string code);
        Task<string> GenerateQrCodeAsync(string secretKey, string email);
    }
}
