using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modulo3.Datos;
using Modulo3.DTOs;
using Modulo3.Utilidades;

namespace Modulo3.Controllers
{
    [Route("api/facturas")]
    [ApiController]
    [Authorize]
    [DeshabilitarLimitarPeticiones]
    public class FacturasController: ControllerBase
    {
        private readonly ApplicationDbContext context;

        public FacturasController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpPost]
        public async Task<ActionResult> Pagar(FacturaPagarDTO facturaPagarDTO)
        {
            var facturaDB = await context.Facturas
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == facturaPagarDTO.FacturaId);

            if (facturaDB is null)
            {
                return NotFound();
            }

            if (facturaDB.Pagada)
            {
                ModelState.AddModelError(nameof(facturaPagarDTO.FacturaId), "La factura ya fue saldada");
                return ValidationProblem();
            }

            // Pretender que el pago fue exitoso:

            facturaDB.Pagada = true;
            await context.SaveChangesAsync();

            // Como puede pasar que pagaron una factura pero tengan otras vencidas debemos corroborar eso:
            var hayFacturasPendientesVencidas = await context.Facturas
                .AnyAsync(x => x.UsuarioId == facturaDB.UsuarioId && 
                !x.Pagada && x.FechaLimiteDePago < DateTime.Today);

            if (!hayFacturasPendientesVencidas)
            {
                facturaDB.Usuario!.MalaPaga = false;
                await context.SaveChangesAsync();
               
            }
            return NoContent();
        }
    }
}
