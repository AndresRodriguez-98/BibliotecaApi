using Microsoft.AspNetCore.Identity;

namespace Modulo3.Servicios
{
    public interface IServicioUsuarios
    {
        Task<IdentityUser?> ObtenerUsuario();
    }
}