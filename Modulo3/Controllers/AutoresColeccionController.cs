using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modulo3.Datos;
using Modulo3.DTOs;
using Modulo3.Entidades;

namespace Modulo3.Controllers
{
    [ApiController]
    [Route("api/autores-coleccion")]
    [Authorize(Policy = "esAdmin")]
    public class AutoresColeccionController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public AutoresColeccionController(ApplicationDbContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet("{ids}", Name = "ObtenerAutoresPorIds")] // api/autores-coleccion/1,2,3
        public async Task<ActionResult<List<AutorConLibrosDTO>>> Get(string ids)
        {
            var idsColeccion = new List<int>();

            foreach (var id in ids.Split(","))
            {
                if (int.TryParse(id, out int idInt))
                {
                    idsColeccion.Add(idInt);
                }
            }

            if (!idsColeccion.Any())
            {
                ModelState.AddModelError(nameof(ids), "Ningún Id fue encontrado");
                return ValidationProblem();
            }

            var autores = await context.Autores
                            .Include(x => x.Libros)
                                .ThenInclude(x => x.Libro)
                            .Where(x => idsColeccion.Contains(x.Id))
                            .ToListAsync();

            if (autores.Count != idsColeccion.Count)
            {
                return NotFound();
            }

            var autoresDTO = mapper.Map<List<AutorConLibrosDTO>>(autores);
            return autoresDTO;
        }

        [HttpPost]
        public async Task<ActionResult> Post(IEnumerable<AutorCreacionDTO> autoresCreacionDTO)
        {
            var autores = mapper.Map<IEnumerable<Autor>>(autoresCreacionDTO);
            // AddRange nos sirve para agregar un listado de entidades que es lo que queremos en este caso:
            context.AddRange(autores);
            await context.SaveChangesAsync();

            // En teoria deberiamos devolver un 201 (Created at raw result) por lo tanto tenemos que hacer esto:
            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
            var ids = autores.Select(x => x.Id);
            var idsString = string.Join(",", ids);
            return CreatedAtRoute("ObtenerAutoresPorIds", new { ids = idsString }, autoresDTO);

            // en lugar de hacer un return Ok();
        }
    }
}
