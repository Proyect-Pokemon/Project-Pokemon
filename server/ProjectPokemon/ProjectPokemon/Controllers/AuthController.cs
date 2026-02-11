using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectPokemon.Helpers;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Database.Repositories;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SocialNetwork.Models.Dtos.Auth;


namespace SocialNetwork.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase {
        // Obtenemos por inyección los parámetros preestablecidos
        // para crear los token
        private readonly UserRepository _userRepository;
        private readonly TokenValidationParameters _tokenParameters;

        public AuthController(UserRepository userRepository, IOptionsMonitor<JwtBearerOptions> jwtOptions) {
            _userRepository = userRepository;
            _tokenParameters = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme)
                .TokenValidationParameters;
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login([FromBody] LoginModel model) {
            // Si el usuario existe entonces creamos y le damos su token
            User? user = await _userRepository.GetUserByNicknameAsync(model.Nickname);

            if (user is null)
                return Unauthorized("Credenciales invalidas");

            if (!PasswordHelper.Verify(model.Password, user.Password))
                return Unauthorized("Credenciales invalidas");

            {
                var tokenDescriptor = new SecurityTokenDescriptor {
                    // Aquí añadimos los datos que sirvan para autorizar al usuario
                    Claims = new Dictionary<string, object>
                        {
                            { "id", user.Id.ToString() },
                            { ClaimTypes.Name, user.Nickname },
                            { ClaimTypes.Role, user.Role },
                            { "AvatarPath", user.AvatarPath! }
                        },
                    // Aquí indicamos cuándo caduca el token
                    Expires = DateTime.UtcNow.AddDays(5),
                    // Aquí especificamos nuestra clave y el algoritmo de firmado
                    SigningCredentials = new SigningCredentials(
                        _tokenParameters.IssuerSigningKey,
                        SecurityAlgorithms.HmacSha256Signature)
                };

                // Creamos el token y se lo devolvemos al usuario logeado
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
                string stringToken = tokenHandler.WriteToken(token);

                return Ok(new { accessToken = stringToken });
            }
        }
    }
}
