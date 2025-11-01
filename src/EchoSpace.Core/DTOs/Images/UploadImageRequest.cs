using EchoSpace.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace EchoSpace.Core.DTOs.Images
{
    /// <summary>
    /// Request DTO for uploading an image
    /// </summary>
    public class UploadImageRequest
    {
        public IFormFile File { get; set; } = null!;
        public ImageSource Source { get; set; }
        public Guid? UserId { get; set; }
        public Guid? PostId { get; set; }
        public string? Description { get; set; }
    }
}

