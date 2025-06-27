using Modulo3.Entidades;

namespace Modulo3.Servicios
{
    public interface IServicioUsuarios
    {
        Task<Usuario?> ObtenerUsuario();
    }
}