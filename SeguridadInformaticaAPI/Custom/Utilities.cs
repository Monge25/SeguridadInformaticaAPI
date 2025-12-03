using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SeguridadInformaticaAPI.Models;

namespace SeguridadInformaticaAPI.Custom
{
    public class Utilities
    {
        private readonly IConfiguration _configuration;

        public Utilities(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Encriptar el password que se recibe de BD
        public string EncryptPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Computar el hash
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convertir el array de bytes a string
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        // Generar JWT
        public string GenerateJWT(User model)
        {
            // Crear la información del usuario para token
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, model.Id.ToString()),
                new Claim(ClaimTypes.Email, model.Email!)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:key"]!));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            // Crear detalle del token
            var jwtConfig = new JwtSecurityToken(
                claims: userClaims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(jwtConfig);
        }
    }
}
