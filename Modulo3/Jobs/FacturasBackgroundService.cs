
using Microsoft.EntityFrameworkCore;
using Modulo3.Datos;

namespace Modulo3.Jobs
{
    public class FacturasBackgroundService : BackgroundService
    {
        private readonly IServiceProvider services;

        // Como en la función EmitirFacturas necesito el ApplicationDbContext, voy a necesitar un scope para ..
        // .. obtener una instancia de éste, ya que el appDbContext tiene un tiempo de vida Scope, por lo tanto..
        // .. necesitamos crear un contexto de ese appDbContext con el IServiceProvider:
        public FacturasBackgroundService(IServiceProvider services)
        {
            this.services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        await EmitirFacturas(context);
                        await SetearUsuariosMalaPaga(context);
                        await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Acá podemos ejecutar un código personalizado al detener la ejecución del job
            }

        }

        private async Task SetearUsuariosMalaPaga(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlAsync($"EXEC Usuarios_SetearMalaPaga");
        }

        private async Task EmitirFacturas(ApplicationDbContext context)
        {
                var hoy = DateTime.Today;
                var fechaComparacion = hoy.AddMonths(-1);

                var facturasDelMesYaFueronEmitidas = await
                    context.FacturasEmitidas.AnyAsync(x => x.Año == fechaComparacion.Year
                    && x.Mes == fechaComparacion.Month);

                if (!facturasDelMesYaFueronEmitidas)
                {
                    var fechaInicio = new DateTime(fechaComparacion.Year, fechaComparacion.Month, 1);
                    var fechaFin = fechaInicio.AddMonths(1);
                    await context.Database.ExecuteSqlAsync($"EXEC Facturas_Crear {fechaInicio.ToString("yyyy-MM-dd")}, {fechaFin.ToString("yyyy-MM-dd")}");
                }
        }
    }
}
