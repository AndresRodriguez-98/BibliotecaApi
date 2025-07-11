﻿using Microsoft.EntityFrameworkCore;
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
            modelBuilder.Entity<Comentario>().HasQueryFilter(b => !b.EstaBorrado);
        }
        
        // con esto creamos la tabla en la Db a partir de las props de Autor
        public DbSet<Autor> Autores { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<AutorLibro> AutoresLibros { get; set; }
        public DbSet<Error> Errores { get; set; }
        public DbSet<LlaveAPI> LlavesAPI { get; set; }
        public DbSet<Peticion> Peticiones { get; set; }
        public DbSet<RestriccionDominio> RestriccionesDominio { get; set; }
        public DbSet<RestriccionIP> RestriccionesIP { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<FacturaEmitida> FacturasEmitidas { get; set; }
    }
}
