using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modulo3.Datos;
using Modulo3.DTOs;
using Modulo3.Entidades;

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

        public AutoresController(ApplicationDbContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<AutorDTO>> Get()
        {
            var autores = await context.Autores.ToListAsync();
            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
            return autoresDTO;
        }

        // obtenemos uno por id con sus libros:
        [HttpGet("{id:int}", Name = "ObtenerAutor")] // api/autores/id
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
        public async Task<IEnumerable<Autor>> GetPorNombre(string nombre)
        {
            return await context.Autores.Where(x => x.Nombres.Contains(nombre)).ToListAsync();
        }


        [HttpPost]
        public async Task<ActionResult> Post(AutorCreacionDTO autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            // Vamos a usar EntityFrameworkCore para "inyectar" un autor en mi tabla de autores (iny.de dependencias):
            context.Add(autor); // Esto NO lo está agregando a la tabla, si no que lo está MARCANDO para ser guardado a futuro
            await context.SaveChangesAsync();
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}")]

        public async Task<ActionResult> Put(int id, AutorCreacionDTO autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;
            context.Update(autor);
            await context.SaveChangesAsync();
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

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var registroBorrados = await context.Autores.Where(x => x.Id == id).ExecuteDeleteAsync();
            // el executeDelete borra el elemento de la tabla si coincide pero a la vez nos devuelve el numero de registros borrados
            // por eso tenemos que aplicar una logica que verifique si borró alguno para devolver un okey o no:

            if (registroBorrados == 0)
            {
                return NotFound();
            }
            return Ok();
        }
    }
}
