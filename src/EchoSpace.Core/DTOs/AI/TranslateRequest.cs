using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs
{
    public record TranslateRequest
    {
        [Required]
        public Guid PostId { get; set; }


        [Required]
        public string Language {get; set;} = "FR";
   
    }
}
