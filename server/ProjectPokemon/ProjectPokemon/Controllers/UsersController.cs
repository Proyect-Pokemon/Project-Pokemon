using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Helpers;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.User;

namespace ProjectPokemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase {
    private readonly UnitOfWork _unitOfWork;

    public UsersController(UnitOfWork unitOfWork) {
        _unitOfWork = unitOfWork;
    }

    // Obtiene el ID del usuario autenticado desde el token JWT
    private int? GetAuthenticatedUserId() {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (int.TryParse(userIdClaim, out int userId)) return userId;
        return null;
    }

    // DELETE: api/users/account
    [Authorize]
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount() {
        int? authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId == null)
            return Unauthorized(new { error = "No se pudo autenticar al usuario." });

        User? user = await _unitOfWork.UserRepository.GetByIdAsync(authenticatedUserId.Value);
        if (user == null)
            return NotFound(new { error = "Usuario no encontrado." });

        // Si el usuario es admin, comprobar que no es el único
        if (user.Role == "admin") {
            var allUsers = await _unitOfWork.UserRepository.GetAllAsync();
            int adminCount = allUsers.Count(u => u.Role == "admin");
            if (adminCount <= 1)
                return BadRequest(new { error = "No puedes eliminar tu cuenta porque eres el único administrador del sistema." });
        }

        await _unitOfWork.UserRepository.DeleteAsync(user);
        bool success = await _unitOfWork.SaveAsync();
        if (!success)
            return StatusCode(500, new { error = "No se pudo eliminar la cuenta." });

        return Ok(new { message = "Cuenta eliminada correctamente." });
    }

    // GET: api/users
    [HttpGet]
    public async Task<IEnumerable<GetUserDto>> GetAllUsers() {
        ICollection<User> users = await _unitOfWork.UserRepository.GetAllAsync();

        return users.Select(user => new GetUserDto {
            Id = user.Id,
            Nickname = user.Nickname,
            AvatarPath = user.AvatarPath!
        });
    }

    // GET: api/users/all?userId=X
    [Authorize]
    [HttpGet("all")]
    public async Task<ActionResult<GetUserProfileExtendDto>> GetProfileUsers(int userId) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

        if (user == null) return NotFound("Usuario no encontrado.");

        return new GetUserProfileExtendDto {
            Email = user.Email,
            Nickname = user.Nickname,
            Biography = user.Biography,
            AvatarPath = user.AvatarPath,
            FavoriteTeamId = user.FavoriteTeamId
        };
    }

    // PUT: api/users/FavoriteTeam?id=X
    [Authorize]
    [HttpPut("FavoriteTeam")]
    public async Task<IActionResult> UpdateFavoriteTeam(int id, [FromBody] PutFavoriteTeamDto dto) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.FavoriteTeamId = dto.FavoriteTeamId;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) return BadRequest();

        return Ok(dto);
    }

    // PUT: api/users/Avatar?id=X
    [Authorize]
    [HttpPut("Avatar")]
    public async Task<IActionResult> UpdateUserAvatar(int id, [FromBody] PutAvatarPathDto dto) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.AvatarPath = dto.AvatarPath;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) return BadRequest();

        return Ok(dto);
    }

    // PUT: api/users/Biography?id=X
    [Authorize]
    [HttpPut("Biography")]
    public async Task<IActionResult> UpdateUserBiography(int id, [FromBody] PutBiographyDto dto) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.Biography = dto.Biography;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) return BadRequest();

        return Ok(dto);
    }

    // PUT: api/users/Password?id=X
    [Authorize]
    [HttpPut("Password")]
    public async Task<IActionResult> UpdateUserPassword(int id, [FromBody] PutPasswordDto dto) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        // Verificar que la contraseña actual es correcta antes de cambiarla
        if (user.Password == null || !PasswordHelper.Verify(dto.CurrentPassword, user.Password)) {
            return BadRequest("La contraseña actual es incorrecta.");
        }

        user.Password = PasswordHelper.Hash(dto.NewPassword);
        await _unitOfWork.UserRepository.UpdateAsync(user);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) return BadRequest();

        return Ok();
    }
}