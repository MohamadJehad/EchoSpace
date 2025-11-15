namespace EchoSpace.Core.DTOs.Tags
{
    public class TagDto
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

