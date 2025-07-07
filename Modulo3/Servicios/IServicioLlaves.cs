using Modulo3.Entidades;

namespace Modulo3.Servicios
{
    public interface IServicioLlaves
    {
        Task<LlaveAPI> CrearLlave(string usuarioId, TipoLlave tipoLlave);
        string GenerarLlave();
    }
}