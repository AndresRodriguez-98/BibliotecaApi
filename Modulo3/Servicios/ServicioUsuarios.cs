using Microsoft.AspNetCore.Identity;

namespace Modulo3.Servicios
{
    // para utilizar el Ppio de Inversión de Dependencias extraemos el servicio como una interfaz para poder depender de una clase abstracta!!!
    public class ServicioUsuarios : IServicioUsuarios
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly IHttpContextAccessor contextAccessor;

        public ServicioUsuarios(UserManager<IdentityUser> userManager, IHttpContextAccessor contextAccessor)
        {
            this.userManager = userManager;
            this.contextAccessor = contextAccessor;
        }

        public async Task<IdentityUser?> ObtenerUsuario()
        {
            var emailClaim = contextAccessor.HttpContext!.User.Claims.Where(x => x.Type == "email").FirstOrDefault();

            if (emailClaim is null)
            {
                return null;
            }

            var email = emailClaim.Value;

            return await userManager.FindByEmailAsync(email);
        }
    }
}
