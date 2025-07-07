using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Modulo3.Datos;
using Modulo3.DTOs;
using Modulo3.Entidades;
using Modulo3.Middlewares;
using Modulo3.Servicios;
using Modulo3.Swagger;
using Modulo3.Utilidades;
using StackExchange.Redis;
using System.Text;

namespace Modulo3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // area de servicios

            // asi seria un cache local, no distribuido
            builder.Services.AddOutputCache(opciones =>
            {
                opciones.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(1);
            });

            //builder.Services.AddStackExchangeRedisOutputCache(opciones =>
            //{
            //    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
            //});

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
            // servicio para almacenar archivos localmente:
            builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
            //agregando el filtro de accion como servicio (ya que lo injectamos con un ServiceFilter)
            builder.Services.AddScoped<MiFiltroDeAccion>();
            builder.Services.AddScoped<FiltroValidacionLibro>();

            builder.Services.AddScoped<IServicioLlaves, ServicioLlaves>();
           
            // servicio para poder acceder al contexto de la aplicación desde cualquier clase:
            builder.Services.AddHttpContextAccessor();

            // config autenticacion:
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

            // config autorizacion:
            builder.Services.AddAuthorization(opciones =>
            {
                // aca configuramos que el rol de administrador va a ser el que tenga el claim "esAdmin" con valor "true"
                opciones.AddPolicy("esAdmin", politica => politica.RequireClaim("esAdmin"));
            });

            // config del swagger
            builder.Services.AddSwaggerGen(opciones =>
            {
                opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Biblioteca API",
                    Description = "Este es un web api para trabajar con datos de autores y libros",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Email = "andresrodriguezab98@gmail.com",
                        Name = "Andrés Rodríguez",
                        Url = new Uri("https://andres-rodriguez98.vercel.app/")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri("https://opensource.org/license/mit")
                    }
                });

                opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });

                opciones.OperationFilter<FiltroAutorizacion>();
            });

            //Esta configuración sirve para inyectar una sección de configuración (appsettings.json)
            //como objeto fuertemente tipado (LimitarPeticionesDTO), validarlo y dejarlo listo para usar
            //en los servicios.

            // acá registramos al DTO como una opcion configurable y le decimos a .NET que queremos configurar
            // un objeto de tipo LimitarPeticionesDTO con valores que vienen del appsettings.json:
            builder.Services.AddOptions<LimitarPeticionesDTO>()
                // acá VINCULAMOS esa opcion con una seccion especifica del appsettings:
                .Bind(builder.Configuration.GetSection(LimitarPeticionesDTO.Seccion))
                // validamos las anotaciones de data como range o required en el DTO:
                .ValidateDataAnnotations()
                // hacemos que la validacion se haga al momento de iniciar la app
                .ValidateOnStart();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (dbContext.Database.IsRelational())
                {
                    dbContext.Database.Migrate();
                }
            }

                // area de middlewares
                app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
                {
                    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var excepcion = exceptionHandlerFeature?.Error!;

                    var error = new Error()
                    {
                        MensajeDeError = excepcion.Message,
                        StrackTrace = excepcion.StackTrace,
                        Fecha = DateTime.UtcNow
                    };

                    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                    dbContext.Add(error);
                    await dbContext.SaveChangesAsync();
                    await Results.InternalServerError(new
                    {
                        tipo = "error",
                        mensaje = "Ha ocurrido un error inesperado",
                        status = 500
                    }).ExecuteAsync(context);
                }));

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseStaticFiles();

            app.UseCors();

            app.UseLimitarPeticiones();

            app.UseOutputCache();

            app.MapControllers();

            Console.WriteLine("Connection string desde config: " + builder.Configuration.GetConnectionString("redis"));

            app.Run();
        }
    }
}
