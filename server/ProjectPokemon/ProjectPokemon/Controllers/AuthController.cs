using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Dtos.Auth;
using ProjectPokemon.Services;

namespace ProjectPokemon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var token = await _authService.LoginAsync(model);

            if (token is null)
                return Unauthorized("Credenciales invalidas");

            return Ok(new AuthResponseDto { AccessToken = token });
        }
    }
}