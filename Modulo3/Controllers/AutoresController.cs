using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modulo3.Datos;
using Modulo3.DTOs;
using Modulo3.Entidades;
using Modulo3.Servicios;
using Modulo3.Utilidades;
using System.Linq.Dynamic.Core;

namespace Modulo3.Controllers
{
    [ApiController]
    [Route("/api/autores")]
    [Authorize(Policy = "esAdmin")]
    public class AutoresController: ControllerBase
    {
        // De esta manera puedo tener acceso al contexto en toda mi clase AutoresController
        // El problema acá es que tengo un acomplamiento FUERTE (no flexible)
        // Mi clase AutoresController está obligada a utilizar la clase ApplicationDbContext
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly ILogger<AutoresController> logger;
        private readonly IOutputCacheStore outputCacheStore;
        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        public AutoresController(ApplicationDbContext context, IMapper mapper,
            IAlmacenadorArchivos almacenadorArchivos, ILogger<AutoresController> logger,
            IOutputCacheStore outputCacheStore
            )
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.logger = logger;
            this.outputCacheStore = outputCacheStore;
        }


        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        [ServiceFilter<MiFiltroDeAccion>()]
        public async Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            ArgumentNullException.ThrowIfNull(paginacionDTO);
            var queryable = context.Autores.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);

            var autores = await queryable
                .OrderBy(x => x.Nombres)
                .Paginar(paginacionDTO).ToListAsync();
            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
            return autoresDTO;
        }

        // obtenemos uno por id con sus libros:
        [HttpGet("{id:int}", Name = "ObtenerAutor")] // api/autores/id
        [AllowAnonymous]
        [EndpointSummary("Obtiene autor por id")]
        [EndpointDescription("Obtiene autores por id, devolviendo tambien sus libros y retornando un 404 en caso de que no exista")]
        [ProducesResponseType<AutorConLibrosDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<AutorConLibrosDTO>> Get(int id) 
        {
            var autor = await context.Autores
                .Include(x => x.Libros)
                    .ThenInclude(x => x.Libro)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (autor is null)
            {
                return NotFound();
            }

            var autorDTO = mapper.Map<AutorConLibrosDTO>(autor);

            return autorDTO;
        }

        // Si yo quiero un nuevo endpoint que agarre un solo autor (el primero) necesito darle una ruta
        // para que no haya dos endopoints que puedan responder a la misma peticion (get) en la misma ruta, esto genera un
        // ERROR DE EXCEPCIÓN AMBIGUA
        [HttpGet("{nombre:alpha}")]
        [AllowAnonymous]
        public async Task<IEnumerable<Autor>> GetPorNombre(string nombre)
        {
            return await context.Autores.Where(x => x.Nombres.Contains(nombre)).ToListAsync();
        }


        [HttpGet("filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult> Filtrar([FromQuery] AutorFiltroDTO autorFiltroDTO)
        {
            var queryable = context.Autores.AsQueryable();

            if (!string.IsNullOrEmpty(autorFiltroDTO.Nombres))
            {
                queryable = queryable.Where(x => x.Nombres.Contains(autorFiltroDTO.Nombres));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.Apellidos))
            {
                queryable = queryable.Where(x => x.Apellidos.Contains(autorFiltroDTO.Apellidos));
            }

            if (autorFiltroDTO.IncluirLibros)
            {
                queryable = queryable.Include(x => x.Libros).ThenInclude(x => x.Libro);
            }

            if (autorFiltroDTO.TieneFoto.HasValue)
            {
                if (autorFiltroDTO.TieneFoto.Value)
                {
                    queryable = queryable.Where(x => x.Foto != null);
                }
                else
                {
                    queryable = queryable.Where(x => x.Foto == null);
                }
            }

            if (autorFiltroDTO.TieneLibros.HasValue)
            {
                if (autorFiltroDTO.TieneLibros.Value)
                {
                    queryable = queryable.Where(x => x.Libros.Any());
                }
                else
                {
                    queryable = queryable.Where(x => !x.Libros.Any());
                }
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.TituloLibro))
            {
                queryable = queryable.Where(x =>
                    x.Libros.Any(y => y.Libro!.Titulo.Contains(autorFiltroDTO.TituloLibro)));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.CampoOrdenar))
            {
                var tipoOrden = autorFiltroDTO.OrdenAscendente ? "ascending" : "descending";

                try
                {
                    queryable = queryable.OrderBy($"{autorFiltroDTO.CampoOrdenar} {tipoOrden}");
                }
                catch (Exception ex)
                {
                    queryable = queryable.OrderBy(x => x.Nombres);
                    logger.LogError(ex.Message, ex);
                }
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Nombres);
            }

                var autores = await queryable
                    .Paginar(autorFiltroDTO.PaginacionDTO).ToListAsync();

            if (autorFiltroDTO.IncluirLibros)
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorConLibrosDTO>>(autores);
                return Ok(autoresDTO);
            }
            else
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
                return Ok(autoresDTO);
            }

        }


        [HttpPost]
        public async Task<ActionResult> Post(AutorCreacionDTO autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            // Vamos a usar EntityFrameworkCore para "inyectar" un autor en mi tabla de autores (iny.de dependencias):
            context.Add(autor); // Esto NO lo está agregando a la tabla, si no que lo está MARCANDO para ser guardado a futuro
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPost("con-foto")]
        public async Task<ActionResult> PostConFoto([FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);

            if(autorCreacionDTO.Foto is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            // Vamos a usar EntityFrameworkCore para "inyectar" un autor en mi tabla de autores (iny.de dependencias):
            context.Add(autor); // Esto NO lo está agregando a la tabla, si no que lo está MARCANDO para ser guardado a futuro
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}")]

        public async Task<ActionResult> Put(int id,[FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var existeAutor = await context.Autores.AnyAsync(x => x.Id == id);
            if (!existeAutor)
            {
                return NotFound();
            }
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;

            if (autorCreacionDTO.Foto is not null)
            {
                var fotoActual = await context.Autores.Where(x => x.Id == id).Select(x => x.Foto).FirstAsync();
                var url = await almacenadorArchivos.Editar(fotoActual, contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            context.Update(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            return NoContent();
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AutorPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var autorDB = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autorDB is null)
            {
                return NotFound();
            }

            // como queremos aplicar los cambios sobre un objeto hacemos esto:
            var autorPatchDTO = mapper.Map<AutorPatchDTO>(autorDB);

            // con esto le aplicams los cambios que vienen del cliente
            patchDoc.ApplyTo(autorPatchDTO, ModelState);

            // intentamos validar nuestro modelo
            var esValido = TryValidateModel(autorPatchDTO);

            // si no es valido retornamos error de validacion:
            if (!esValido)
            {
                return ValidationProblem();
            }
            // sino lo MAPPIAMOS a autorDB para mandarlo a la db
            mapper.Map(autorPatchDTO, autorDB);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var autor = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autor is null)
            {
                return NotFound();
            }

            context.Remove(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            await almacenadorArchivos.Borrar(autor.Foto, contenedor);

            return NoContent();
        }
    }
}
