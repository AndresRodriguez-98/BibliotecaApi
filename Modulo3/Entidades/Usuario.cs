using Microsoft.AspNetCore.Identity;

namespace Modulo3.Entidades
{
    public class Usuario: IdentityUser
    {
        public DateTime FechaNacimiento { get; set; }
    }
}
