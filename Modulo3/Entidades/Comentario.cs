using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Modulo3.Entidades
{
    public class Comentario
    {
        public Guid Id { get; set; }
        [Required]
        public required string Cuerpo { get; set; }
        public DateTime FechaPublicacion { get; set; }
        public int LibroId { get; set; }
        public Libro? Libro { get; set; }
        public required string UsuarioId { get; set; }
        // lo hacemos nullable ya que no siempre vamos a tener la data relacionada:
        public Usuario? Usuario { get; set; }
    }
}
