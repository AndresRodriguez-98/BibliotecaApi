using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Modulo3.Datos;
using Modulo3.DTOs;
using Modulo3.Entidades;
using Modulo3.Servicios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Modulo3.Controllers
{
    [ApiController]
    [Route("/api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<Usuario> userManager;
        private readonly IConfiguration configuration;
        private readonly SignInManager<Usuario> signInManager;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public UsuariosController(UserManager<Usuario> userManager,
            IConfiguration configuration, SignInManager<Usuario> signInManager,
            IServicioUsuarios servicioUsuarios, ApplicationDbContext context,
            IMapper mapper)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.signInManager = signInManager;
            this.servicioUsuarios = servicioUsuarios;
            this.context = context;
            this.mapper = mapper;
        }


        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO
            credencialesUsuarioDTO)
        {
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);
            if (usuario == null) 
            {
                // aca tenemos que ser lo mas vagos posibles para no darle data a nadie por razones de seguridad
                return RetornarLoginIncorrecto();
            }

            // checkeo que la contraseña hasheada sea igual a la que tiene que tener
            var resultado = await signInManager.CheckPasswordSignInAsync(usuario, 
                credencialesUsuarioDTO.Password!, lockoutOnFailure: false);

            // si coinciden las credenciales, creo el token, sino retorno login incorrecto:
            if (resultado.Succeeded)
            {
                return await ConstruirToken(credencialesUsuarioDTO);
            }
            else
            {
                return RetornarLoginIncorrecto();
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<UsuarioDTO>> Get()
        {
            var usuarios = await context.Users.ToListAsync();
            // mapeamos los usuarios a DTOs:
            var usuariosDTO = mapper.Map<IEnumerable<UsuarioDTO>>(usuarios);
            return usuariosDTO;
        }

        [HttpPut]
        [Authorize]
        public async Task<ActionResult> ActualizarUsuario(ActualizarUsuarioDTO actualizarUsuarioDTO)
        {
            // obtenemos el usuario logueado:
            var usuario = await servicioUsuarios.ObtenerUsuario();
            if (usuario == null)
            {
                return NotFound();
            }
            // actualizamos los datos del usuario logueado:
            usuario.FechaNacimiento = actualizarUsuarioDTO.FechaNacimiento;
            // guardamos los cambios en la DB:
            await userManager.UpdateAsync(usuario);
            return NoContent();
        }

        [HttpGet("renovar-token")]
        [Authorize]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> RenovarToken()
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();
            if (usuario == null)
            {
                return NotFound();
            }

            var credencialesUsuarioDTO = new CredencialesUsuarioDTO { Email = usuario.Email! };

            var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);

            return respuestaAutenticacion;
        }

        [HttpPost("hacer-admin")]
        [Authorize(Policy = "esAdmin")]
        [AllowAnonymous]
        public async Task<ActionResult> HacerAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);
            if (usuario == null)
            {
                return NotFound();
            }
            // le agregamos el claim de admin al usuario:
            await userManager.AddClaimAsync(usuario, new Claim("esAdmin", "true"));

            return NoContent();
        }

        [HttpPost("remover-admin")]
        [Authorize(Policy = "esAdmin")]
        public async Task<ActionResult> RemoverAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);
            if (usuario == null)
            {
                return NotFound();
            }
            // le agregamos el claim de admin al usuario:
            await userManager.RemoveClaimAsync(usuario, new Claim("esAdmin", "true"));
            return NoContent();
        }


        private ActionResult RetornarLoginIncorrecto()
        {
            ModelState.AddModelError(string.Empty, "Login incorrecto");
            return ValidationProblem();
        }

        [HttpPost("registro")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new Usuario
            {
                UserName = credencialesUsuarioDTO.Email,
                Email = credencialesUsuarioDTO.Email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password!);

            if (resultado.Succeeded)
            {
                var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);
                return respuestaAutenticacion;
            }
            else
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return ValidationProblem();
            }
        }
        private async Task<RespuestaAutenticacionDTO> ConstruirToken(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            // lo primero que quiero es crear los Claims:
            var claims = new List<Claim>
            {
                new Claim("email", credencialesUsuarioDTO.Email),
                new Claim("clave", "valor")
            };

            // Despues queremos ir a buscar el usuario a la DB, y agarrar los claims que tenga:
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);
            var claimsDB = await userManager.GetClaimsAsync(usuario!);
            claims.AddRange(claimsDB);

            // Ahora necesitamos trabajar con la secret key:
            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["llavejwt"]!));
            // le agregamos el algoritmo para firmar el jwt y que no sea adulterable o accedible:
            var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);
            // fecha de caducidad del token:
            var expiracion = DateTime.UtcNow.AddYears(1);
            // creamos el token pasandole lo que necesita el header el body y las credentials:
            var tokenDeSeguridad = new JwtSecurityToken(issuer: null, audience: null, claims: claims, 
                expires: expiracion, signingCredentials: credenciales);
            // Por ultimo SERIALIZAMOS el token (lo pasamos a string) para poder transmitirlo
            // .. y asi poder devolverlo en el response
            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            // Por ultimo retornamos la respuesta de autenticacion con el token ya firmado en nuestro DTO:
            return new RespuestaAutenticacionDTO
            { 
                Token = token, 
                Expiracion = expiracion
            };
        }
    } }
