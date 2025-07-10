using System.ComponentModel.DataAnnotations;

namespace Modulo3.DTOs
{
    public class RestriccionDominioCreacionDTO
    {
        public int LlaveId { get; set; }
        [Required]
        public required string Dominio { get; set; }
    }
}
