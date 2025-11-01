using EchoSpace.Core.Enums;

namespace EchoSpace.Core.DTOs.Images
{
    /// <summary>
    /// Data Transfer Object for Image entity
    /// </summary>
    public class ImageDto
    {
        public Guid ImageId { get; set; }
        public ImageSource Source { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public string? Url { get; set; }
        public Guid? UserId { get; set; }
        public Guid? PostId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

