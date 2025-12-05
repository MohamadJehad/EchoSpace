using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs
{
    public record SummarizeRequest
    {
        [Required]
        public Guid PostId { get; set; }
   
    }
}
