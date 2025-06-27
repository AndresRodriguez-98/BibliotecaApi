using System.ComponentModel.DataAnnotations;

namespace Modulo3.DTOs
{
    public class EditarClaimDTO
    {
        [EmailAddress]
        [Required]
        public required string Email { get; set; }
    }
}
