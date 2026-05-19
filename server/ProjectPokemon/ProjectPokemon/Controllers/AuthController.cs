using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Enum;
using ProjectPokemon.Models.Dtos.Auth;
using ProjectPokemon.Models.Dtos.User;
using ProjectPokemon.Services.Auth;

namespace ProjectPokemon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly GoogleAuthService _googleAuthService;

        public AuthController(AuthService authService, GoogleAuthService googleAuthService)
        {
            _authService = authService;
            _googleAuthService = googleAuthService;
        }

        public class GoogleLoginDto
        {
            public string IdToken { get; set; } = "";
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var token = await _authService.LoginAsync(model);

            if (token is null)
                return Unauthorized("Credenciales invalidas");

            return Ok(new AuthResponseDto
            {
                AccessToken = token
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] AddUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string? error = await _authService.CheckUserExists(dto.Email, dto.Nickname);

            if (!string.IsNullOrEmpty(error))
                return BadRequest(error);

            RegisterResult result = await _authService.RegisterAsync(dto);

            return result switch
            {
                RegisterResult.Success => Ok(),
                RegisterResult.EmailAlreadyExists => BadRequest("El email ya existe."),
                RegisterResult.NicknameAlreadyExists => BadRequest("El nickname ya existe."),
                RegisterResult.InvalidData => BadRequest("Datos inválidos."),
                _ => BadRequest("No se pudo registrar el usuario.")
            };
        }

        [HttpPost("google")]
        public async Task<ActionResult<AuthResponseDto>>
        GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var token =
                await _googleAuthService
                .LoginWithGoogleAsync(dto.IdToken);
            if (token is null)
                return Unauthorized("Google token inválido");

            return Ok(new AuthResponseDto
            {
                AccessToken = token
            });
        }
    }
}