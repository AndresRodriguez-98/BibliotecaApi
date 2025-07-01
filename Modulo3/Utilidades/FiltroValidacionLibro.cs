using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Modulo3.Datos;
using Modulo3.DTOs;

namespace Modulo3.Utilidades
{
    public class FiltroValidacionLibro : IAsyncActionFilter
    {
        private readonly ApplicationDbContext dbContext;

        public FiltroValidacionLibro(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ActionArguments.TryGetValue("libroCreacionDTO", out var value) ||
                    value is not LibroCreacionDTO libroCreacionDTO)
            {
                context.ModelState.AddModelError(string.Empty, "El modelo enviado no es válido");
                // ahora utilizamos un filtro de resultado que DETIENE la ejecucion de todo el pipeline para ..
                // .. pasar el modelo de error a un problem detail antes de la ejecucion de la accion:
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            if (libroCreacionDTO.AutoresIds is null || libroCreacionDTO.AutoresIds.Count == 0)
            {
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds),
                    "No se puede crear un libro sin autores");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            var autoresIdsExisten = await dbContext.Autores
                                    .Where(x => libroCreacionDTO.AutoresIds.Contains(x.Id))
                                    .Select(x => x.Id).ToListAsync();

            if (autoresIdsExisten.Count != libroCreacionDTO.AutoresIds.Count)
            {
                var autoresNoExisten = libroCreacionDTO.AutoresIds.Except(autoresIdsExisten);
                var autoresNoExistenString = string.Join(",", autoresNoExisten);
                var mensajeDeError = $"Los siguientes autores no existen: {autoresNoExistenString}";
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds),
                    mensajeDeError);
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            // si pasa todas las validaciones seguimos en el pipeline
            await next();
        }
    }
}
