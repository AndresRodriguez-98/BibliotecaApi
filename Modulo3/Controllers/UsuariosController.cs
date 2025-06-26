using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Modulo3.DTOs;
using Modulo3.Servicios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Modulo3.Controllers
{
    [ApiController]
    [Route("/api/usuarios")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly IConfiguration configuration;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IServicioUsuarios servicioUsuarios;

        public UsuariosController(UserManager<IdentityUser> userManager,
            IConfiguration configuration, SignInManager<IdentityUser> signInManager,
            IServicioUsuarios servicioUsuarios)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.signInManager = signInManager;
            this.servicioUsuarios = servicioUsuarios;
        }

        [AllowAnonymous]
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

        [HttpGet("renovar-token")]
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

        private ActionResult RetornarLoginIncorrecto()
        {
            ModelState.AddModelError(string.Empty, "Login incorrecto");
            return ValidationProblem();
        }

        [AllowAnonymous]
        [HttpPost("registro")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new IdentityUser
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
