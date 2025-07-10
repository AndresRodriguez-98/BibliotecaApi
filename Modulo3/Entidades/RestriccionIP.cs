using System.ComponentModel.DataAnnotations;

namespace Modulo3.Entidades
{
    public class RestriccionIP
    {
        public int Id { get; set; }
        public int LlaveId { get; set; }
        [Required]
        public required string IP { get; set; }
        public LlaveAPI? Llave { get; set; }
    }
}
