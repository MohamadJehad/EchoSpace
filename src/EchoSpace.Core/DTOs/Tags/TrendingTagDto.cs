namespace EchoSpace.Core.DTOs.Tags
{
    public class TrendingTagDto
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int PostsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

