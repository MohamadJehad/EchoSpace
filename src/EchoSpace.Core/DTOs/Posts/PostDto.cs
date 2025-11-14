namespace EchoSpace.Core.DTOs.Posts
{
    public class PostDto
    {
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        
        // Author information
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public string AuthorUserName { get; set; } = string.Empty;
        
        // Tag information
        public List<TagInfoDto> Tags { get; set; } = new List<TagInfoDto>();
    }

    public class TagInfoDto
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
    }
}
