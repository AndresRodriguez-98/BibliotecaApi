using BibliotecaAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace Modulo3.DTOs
{
    public class AutorCreacionDTO
    {
        [Required(ErrorMessage = "El campo {0} es requerido")] // esto es para ASP, para decirle que si llega un autor sin nombre que no ejecute la peticion
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        [PrimeraLetraMayuscula]
        public required string Apellidos { get; set; } // en cambio aca el required es de C#

        [Required(ErrorMessage = "El campo {0} es requerido")] // esto es para ASP, para decirle que si llega un autor sin nombre que no ejecute la peticion
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        [PrimeraLetraMayuscula]
        public required string Nombres { get; set; } // en cambio aca el required es de C#

        [StringLength(20, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        public string? Identificacion { get; set; }
        public List<LibroCreacionDTO> Libros { get; set; } = [];
    }
}
