using System.ComponentModel.DataAnnotations;

namespace Modulo3.DTOs
{
    public class RestriccionDominioActualizacionDTO
    {
        [Required]
        public required string Dominio { get; set; }
    }
}
