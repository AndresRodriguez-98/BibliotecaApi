using System.ComponentModel.DataAnnotations;

namespace Modulo3.DTOs
{
    public class ComentarioCreacionDTO
    {
        [Required]
        public required string Cuerpo { get; set; }
    }
}
