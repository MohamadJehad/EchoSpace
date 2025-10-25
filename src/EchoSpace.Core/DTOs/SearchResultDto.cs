namespace EchoSpace.Core.DTOs
{
    /// <summary>
    /// Data transfer object for search results.
    /// </summary>
    public class SearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public double MatchScore { get; set; }
    }
}

