using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modulo3.Datos;
using Modulo3.DTOs;
using Modulo3.Entidades;
using Modulo3.Servicios;
using Modulo3.Utilidades;

namespace Modulo3.Controllers
{
    [Route("api/llavesapi")]
    [Authorize]
    [ApiController]
    [DeshabilitarLimitarPeticiones]
    public class LlavesAPIController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IServicioLlaves servicioLlaves;
        private readonly IServicioUsuarios servicioUsuarios;

        public LlavesAPIController(ApplicationDbContext context,
            IMapper mapper, IServicioLlaves servicioLlaves, IServicioUsuarios servicioUsuarios)
        {
            this.context = context;
            this.mapper = mapper;
            this.servicioLlaves = servicioLlaves;
            this.servicioUsuarios = servicioUsuarios;
        }

        [HttpGet]
        public async Task<IEnumerable<LlaveDTO>> Get()
        {
            var usuarioId = servicioUsuarios.ObtenerIdUsuario();
            var llaves = await context.LlavesAPI
                .Include(x => x.RestriccionesDominio)
                .Where(x => x.UsuarioId == usuarioId).ToListAsync();
            return mapper.Map<IEnumerable<LlaveDTO>>(llaves);
        }

        [HttpGet("{id:int}", Name = "ObtenerLlaves")]
        public async Task<ActionResult<LlaveDTO>> Get(int id)
        {
            var usuarioId = servicioUsuarios.ObtenerIdUsuario();
            var llave = await context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == id);

            if (llave is null)
            {
                return NotFound();
            }

            if (llave.UsuarioId != usuarioId)
            {
                return Forbid();
            }

            return mapper.Map<LlaveDTO>(llave);
        }

        [HttpPost]
        public async Task<ActionResult> Post(LlaveCreacionDTO llaveCreacionDTO)
        {
            var usuarioId = servicioUsuarios.ObtenerIdUsuario()!;

            if (llaveCreacionDTO.TipoLlave == TipoLlave.Gratuita)
            {
                var elUsuarioYaTieneLlaveGratuita = await context
                    .LlavesAPI.AnyAsync(x => x.UsuarioId == usuarioId && x.TipoLlave == TipoLlave.Gratuita);

                if (elUsuarioYaTieneLlaveGratuita)
                {
                    ModelState.AddModelError(nameof(llaveCreacionDTO.TipoLlave),
                        "El usuario ya tiene una llave gratuita");
                    return ValidationProblem();
                }
            }

            var llaveAPI = await servicioLlaves.CrearLlave(usuarioId, llaveCreacionDTO.TipoLlave);
            var llaveDTO = mapper.Map<LlaveDTO>(llaveAPI);
            return CreatedAtRoute("ObtenerLlaves", new { id = llaveAPI.Id }, llaveDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, LlaveActualizacionDTO llaveActualizacionDTO)
        {
            var usuarioId = servicioUsuarios.ObtenerIdUsuario();

            var llaveDB = await context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == id);

            if (llaveDB is null)
            {
                return NotFound();
            }

            if (usuarioId != llaveDB.UsuarioId)
            {
                return Forbid();
            }

            if (llaveActualizacionDTO.ActualizarLlave)
            {
                llaveDB.Llave = servicioLlaves.GenerarLlave();
            }

            llaveDB.Activa = llaveActualizacionDTO.Activa;
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var llaveDB = await context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == id);

            if (llaveDB is null)
            {
                return NotFound();
            }

            var usuarioId = servicioUsuarios.ObtenerIdUsuario();

            if (usuarioId != llaveDB.UsuarioId)
            {
                return Forbid();
            }

            if (llaveDB.TipoLlave == TipoLlave.Gratuita)
            {
                ModelState.AddModelError("", "No puedes borrar una llave gratuita");
                return ValidationProblem();
            }

            context.Remove(llaveDB);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
