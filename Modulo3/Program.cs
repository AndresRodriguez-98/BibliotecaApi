using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Modulo3.Datos;
using Modulo3.Entidades;
using Modulo3.Servicios;
using System.Text;

namespace Modulo3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // area de servicios

            var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

            builder.Services.AddCors(opciones =>
            {
                opciones.AddDefaultPolicy(opcionesCORS =>
                {
                    opcionesCORS.WithOrigins(origenesPermitidos).AllowAnyMethod().AllowAnyHeader();
                });
            });
            

            builder.Services.AddAutoMapper(typeof(Program));
            
            builder.Services.AddControllers().AddNewtonsoftJson();

            // configuramos el contexto de la db como un servicio dandole como nombre el que establecimos
            builder.Services.AddDbContext<ApplicationDbContext>(opciones => opciones.UseSqlServer("name=DefaultConnection"));

            // servicio para el Identity (utilizamos el IdentityUser para tener el sistema de usuarios
            // le agregamos el ApplicationDbContext para que pueda conectarse a nuestra base de datos
            // y por ultimo le agregamos el proveedor de token
            builder.Services.AddIdentityCore<Usuario>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // servicio para poder registrar usuarios a traves de UserManager
            builder.Services.AddScoped<UserManager<Usuario>>();
            // servicio para autenticar a los usuarios a traves del SignInManager
            builder.Services.AddScoped<SignInManager<Usuario>>();
            // servicio para obtener el usuarioId del claim de nuestro jwt (usamos transient ya que no necesito compartir estado):
            builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();



            // servicio para poder acceder al contexto de la aplicación desde cualquier clase:
            builder.Services.AddHttpContextAccessor();

            // por ultimo tenemos que configurar la autenticación para que labure con JWT:
            builder.Services.AddAuthentication().AddJwtBearer(opciones =>
            {
                // aca hacemos que ASP no nos cambie el nombre de un claim por otro, osea si nosotros lo llamamos "email" que no lo cambie a otro
                opciones.MapInboundClaims = false;

                // aca configuramos que vamos a tener en cuenta a la hora de validar un token:
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // aca configuramos la llave, sacandola desde un proveedor de configuracion nuestro como una var de amb.
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization(opciones =>
            {
                // aca configuramos que el rol de administrador va a ser el que tenga el claim "esAdmin" con valor "true"
                opciones.AddPolicy("esAdmin", politica => politica.RequireClaim("esAdmin"));
            });



            var app = builder.Build();

            // area de middlewares
            app.UseCors();

            app.MapControllers();

            app.Run();
        }
    }
}
