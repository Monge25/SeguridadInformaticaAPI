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
        public async Task<IActionResult> SignUp(UserDTO model)
        {
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
                return StatusCode(StatusCodes.Status200OK, new { isSucces = false, token = "" });
            else
                return StatusCode(StatusCodes.Status200OK, new { isSucces = true, token = _utilities.GenerateJWT(userFound) });
        }
    }
}
