using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Tools.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EchoSpace.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly EchoSpaceDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailSender _emailSender;

        public NotificationService(EchoSpaceDbContext context, IConfiguration configuration, ILogger<AuthService> logger, IEmailSender emailSender)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task SendFollowEmailAsync(User follower, User followed)
        {
            try
            {
                var subject = $"{follower.Name} started following you!";
                var emailBody = $@"
                <html>
                <body style='font-family: Arial; color:#333;'>
                    <h2>Hello {followed.Name},</h2>
                    <p><strong>{follower.Name}</strong> just started following you on EchoSpace.</p>
                    <p>Keep sharing your thoughts and grow your network!</p>
                    <hr/>
                    <p style='font-size:12px;color:#777;'>EchoSpace Team</p>
                </body>
                </html>";

                await _emailSender.SendEmailAsync(followed.Email, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending follow notification email to {Email}", followed.Email);
            }
        }

        public async Task SendFollowedPostNotificationEmailAsync(User follower, User followed, Post post)
        {
            try
            {
                var subject = $"{followed.Name} posted something new!";
                var emailBody = $@"
                <html>
                <body style='font-family: Arial; color:#333;'>
                    <h2>Hello {follower.Name},</h2>
                    <p><strong>{followed.Name}</strong> just shared a new post:</p>
                    <blockquote style='border-left:4px solid #007bff;padding-left:10px;color:#555;'>{post.Content}</blockquote>
                    <hr/>
                    <p style='font-size:12px;color:#777;'>EchoSpace Team</p>
                </body>
                </html>";

                await _emailSender.SendEmailAsync(follower.Email, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending followed post notification email to {Email}", follower.Email);
            }
        }

        public async Task SendLikeNotificationEmailAsync(User postOwner, User liker, Post post)
        {
            try
            {
                var subject = $"{liker.Name} liked your post!";
                var emailBody = $@"
                <html>
                <body style='font-family: Arial; color:#333;'>
                    <h2>Hello {postOwner.Name},</h2>
                    <p><strong>{liker.Name}</strong> liked your post:</p>
                    <blockquote style='border-left:4px solid #007bff;padding-left:10px;color:#555;'>{post.Content}</blockquote>
                    <hr/>
                    <p style='font-size:12px;color:#777;'>EchoSpace Team</p>
                </body>
                </html>";

                await _emailSender.SendEmailAsync(postOwner.Email, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending like notification email to {Email}", postOwner.Email);
            }
        }

        public async Task SendCommentNotificationEmailAsync(User postOwner, User commenter, Comment comment)
        {
            try
            {
                var subject = $"{commenter.Name} commented on your post!";
                var emailBody = $@"
                <html>
                <body style='font-family: Arial; color:#333;'>
                    <h2>Hello {postOwner.Name},</h2>
                    <p><strong>{commenter.Name}</strong> commented on your post:</p>
                    <blockquote style='border-left:4px solid #007bff;padding-left:10px;color:#555;'>{comment.Content}</blockquote>
                   
                    <hr/>
                    <p style='font-size:12px;color:#777;'>EchoSpace Team</p>
                </body>
                </html>";

                await _emailSender.SendEmailAsync(postOwner.Email, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending comment notification email to {Email}", postOwner.Email);
            }
        }

    }
}
