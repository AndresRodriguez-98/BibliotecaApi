using Modulo3.Entidades;

namespace Modulo3.Servicios
{
    public interface IServicioUsuarios
    {
        string? ObtenerIdUsuario();
        Task<Usuario?> ObtenerUsuario();
    }
}