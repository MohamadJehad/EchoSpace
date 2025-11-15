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
        public bool IsFollowingAuthor { get; set; }
        
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

    public class ReportedPostDto
    {
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ReportCount { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        
        // Author information
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public string AuthorUserName { get; set; } = string.Empty;
        
        // Report information
        public List<ReportInfoDto> Reports { get; set; } = new List<ReportInfoDto>();
    }

    public class ReportInfoDto
    {
        public Guid ReportId { get; set; }
        public Guid UserId { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
    }
}
