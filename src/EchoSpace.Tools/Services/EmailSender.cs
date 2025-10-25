using EchoSpace.Tools.Interfaces;
using EchoSpace.Tools.Email;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EchoSpace.Tools.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailSender(ILogger<EmailSender> logger, IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            _logger.LogInformation("Sending email to {Email} with subject: {Subject}", to, subject);
        
            // Create the email message
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            // Set the body with both HTML and plain text versions
            var builder = new BodyBuilder();
            builder.HtmlBody = body;
            
            // Create a plain text version by stripping HTML tags
            var plainTextBody = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]*>", "")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Trim();
            
            builder.TextBody = plainTextBody;
            email.Body = builder.ToMessageBody();

            // Create the SMTP client and send the email
            using (var smtp = new SmtpClient())
            {
                try
                {
                    // Connect to the Gmail SMTP server
                    // Port 587 uses STARTTLS (SecureSocketOptions.StartTls)
                    await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
                    
                    // Authenticate with your email and App Password
                    await smtp.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password);
                    
                    // Send the email
                    await smtp.SendAsync(email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}", to);
                    throw;
                }
                finally
                {
                    await smtp.DisconnectAsync(true);
                }
            }

            _logger.LogInformation("Email sent successfully to {Email}", to);
        }

        public async Task SendEmailAsync(string to, string cc, string subject, string body)
        {
            _logger.LogInformation("Sending email to {Email} with CC {CC} and subject: {Subject}", to, cc, subject);
            
            // Create the email message
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Cc.Add(MailboxAddress.Parse(cc));
            email.Subject = subject;

            // Set the body with both HTML and plain text versions
            var builder = new BodyBuilder();
            builder.HtmlBody = body;
            
            // Create a plain text version by stripping HTML tags
            var plainTextBody = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]*>", "")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Trim();
            
            builder.TextBody = plainTextBody;
            email.Body = builder.ToMessageBody();

            // Create the SMTP client and send the email
            using (var smtp = new SmtpClient())
            {
                try
                {
                    await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
                    await smtp.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password);
                    await smtp.SendAsync(email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email} with CC {CC}", to, cc);
                    throw;
                }
                finally
                {
                    await smtp.DisconnectAsync(true);
                }
            }
            
            _logger.LogInformation("Email sent successfully to {Email} with CC {CC}", to, cc);
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body)
        {
            var recipientList = recipients.ToList();
            _logger.LogInformation("Sending bulk email to {Count} recipients with subject: {Subject}", recipientList.Count, subject);
            
            foreach (var recipient in recipientList)
            {
                try
                {
                    await SendEmailAsync(recipient, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email} in bulk operation", recipient);
                    // Continue with other recipients even if one fails
                }
            }
            
            _logger.LogInformation("Bulk email operation completed for {Count} recipients", recipientList.Count);
        }
    }
}
