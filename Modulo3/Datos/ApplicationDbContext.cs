using Microsoft.EntityFrameworkCore;
using Modulo3.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Modulo3.Datos
{
    // aca van las config para EntityFramework
    public class ApplicationDbContext: IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions options): base(options)
        {
        }

        // Esta seria otra manera de agregar propertys a un modelo ya creado en la DB pero desde nuestro context, es mas potente.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        
        // con esto creamos la tabla en la Db a partir de las props de Autor
        public DbSet<Autor> Autores { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<AutorLibro> AutoresLibros { get; set; }
    }
}
