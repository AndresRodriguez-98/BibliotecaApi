using Microsoft.EntityFrameworkCore;

namespace Modulo3.Entidades
{
    [PrimaryKey(nameof(AutorId), nameof(LibroId))]
    public class AutorLibro
    {
        public int AutorId { get; set; }
        public int LibroId { get; set; }
        public int Orden { get; set; }
        public Autor? Autor { get; set; }
        public Libro? Libro { get; set; }
    }
}