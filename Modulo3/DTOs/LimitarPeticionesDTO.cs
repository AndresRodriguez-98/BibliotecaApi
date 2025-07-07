using System.ComponentModel.DataAnnotations;

namespace Modulo3.DTOs
{
    public class LimitarPeticionesDTO
    {
        public const string Seccion = "limitarPeticiones";
        [Required]
        public int PeticionesPorDiaGratuito { get; set; }
    }
}
