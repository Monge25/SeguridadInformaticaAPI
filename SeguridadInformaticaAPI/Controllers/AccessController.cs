using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeguridadInformaticaAPI.Custom;
using SeguridadInformaticaAPI.Models;
using SeguridadInformaticaAPI.Models.DTOs;

namespace SeguridadInformaticaAPI.Controllers
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiController]
    public class AccessController : ControllerBase
    {
        private readonly SeguridadInformaticaDbContext _dbSeguridadInformaticaContext;
        private readonly Utilities _utilities;

        public AccessController(SeguridadInformaticaDbContext dbSeguridadInformaticaContext, Utilities utilities)
        {
            _dbSeguridadInformaticaContext = dbSeguridadInformaticaContext;
            _utilities = utilities;
        }

        [HttpPost]
        [Route("SignUp")]
        public async Task<IActionResult> SignUp([FromBody] UserDTO model)
        {
            if (model == null)
                return BadRequest(new { message = "Body vacío o inválido" });

            if (string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Todos los campos son obligatorios" });
            }

            // Validar si ya existe el email
            var existingUser = await _dbSeguridadInformaticaContext.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null)
                return BadRequest(new { message = "El email ya está registrado" });

            var modelUser = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = _utilities.EncryptPassword(model.Password)
            };

            await _dbSeguridadInformaticaContext.Users.AddAsync(modelUser);
            await _dbSeguridadInformaticaContext.SaveChangesAsync();

            if (modelUser.Id != 0)
                return StatusCode(StatusCodes.Status200OK, new { isSuccess = true });
            else
                return StatusCode(StatusCodes.Status200OK, new { isSuccess = false });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            var userFound = await _dbSeguridadInformaticaContext.Users
                .Where(u => u.Email == model.Email && u.Password == _utilities.EncryptPassword(model.Password)
                ).FirstOrDefaultAsync();

            if (userFound == null)
                return StatusCode(StatusCodes.Status200OK, new { isSucces = false, /* token = "" */ });

            var token = _utilities.GenerateJWT(userFound);

            // Crear cookie segura
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // usar HTTPS
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddHours(1)
            });

            return StatusCode(StatusCodes.Status200OK, new { isSucces = true /* token = _utilities.GenerateJWT(userFound) */ });
        }

        [HttpPost]
        [Route("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Append("jwt", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1) // expira inmediatamente
            });

            return Ok(new { message = "Sesión cerrada correctamente" });
        }

        [Authorize]
        [HttpGet]
        [Route("Validate")]
        public IActionResult Validate()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return Unauthorized(new { isAuthenticated = false });

            return Ok(new { isAuthenticated = true, user = User.Identity.Name });
        }
    }
}
