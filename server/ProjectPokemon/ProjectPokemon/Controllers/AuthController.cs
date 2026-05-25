using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Enum;
using ProjectPokemon.Models.Dtos.Auth;
using ProjectPokemon.Models.Dtos.User;
using ProjectPokemon.Services.Auth;

namespace ProjectPokemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly GoogleAuthService _googleAuthService;

    public AuthController(
        AuthService authService,
        GoogleAuthService googleAuthService)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        string? token =
            await _authService.LoginAsync(model);

        if (token is null)
        {
            return Unauthorized("Credenciales inválidas");
        }

        return Ok(new AuthResponseDto
        {
            AccessToken = token
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(
        [FromBody] AddUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        string? error =
            await _authService.CheckUserExists(
                dto.Email,
                dto.Nickname
            );

        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(error);
        }

        RegisterResult result =
            await _authService.RegisterAsync(dto);

        return result switch
        {
            RegisterResult.Success =>
                Ok(),

            RegisterResult.EmailAlreadyExists =>
                Conflict("El email ya existe."),

            RegisterResult.NicknameAlreadyExists =>
                Conflict("El nickname ya existe."),

            RegisterResult.InvalidData =>
                BadRequest("Datos inválidos."),

            _ =>
                StatusCode(500, "No se pudo registrar el usuario.")
        };
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin(
        [FromBody] GoogleLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdToken))
        {
            return BadRequest("Token inválido");
        }

        string? token =
            await _googleAuthService.LoginWithGoogleAsync(dto.IdToken);

        if (token is null)
        {
            return Unauthorized("Google token inválido");
        }

        return Ok(new AuthResponseDto
        {
            AccessToken = token
        });
    }

    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }
}