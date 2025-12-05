using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Posts
{
    public class ReportPostRequest
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}

