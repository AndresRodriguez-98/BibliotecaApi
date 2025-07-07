using Modulo3.Datos;
using Modulo3.Entidades;

namespace Modulo3.Servicios
{
    public class ServicioLlaves : IServicioLlaves
    {
        private readonly ApplicationDbContext context;

        public ServicioLlaves(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<LlaveAPI> CrearLlave(string usuarioId, TipoLlave tipoLlave)
        {
            var llave = GenerarLlave();
            var llaveAPI = new LlaveAPI
            {
                Llave = llave,
                TipoLlave = tipoLlave,
                Activa = true,
                UsuarioId = usuarioId
            };

            context.Add(llaveAPI);
            await context.SaveChangesAsync();
            return llaveAPI;
        }

        public string GenerarLlave() => Guid.NewGuid().ToString().Replace("-", "");
    }
}