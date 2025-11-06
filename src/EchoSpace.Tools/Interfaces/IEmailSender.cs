namespace EchoSpace.Tools.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailAsync(string to, string cc, string subject, string body);
        Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body);

    }
}
