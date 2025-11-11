using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Entities;

namespace EchoSpace.Core.Interfaces
{
    public interface INotificationService
    {

        Task SendFollowEmailAsync(User follower, User followed);

        Task SendFollowedPostNotificationEmailAsync(User follower, User followed, Post post);
        
        Task SendLikeNotificationEmailAsync(User postOwner, User Liker, Post post);

        Task SendCommentNotificationEmailAsync(User postOwner, User commenter, Comment comment);
    }
}

