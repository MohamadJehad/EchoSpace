namespace EchoSpace.Core.DTOs.Auth
{
    public class TotpSetupResponse
    {
        public string QrCodeUrl { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
    }
}
