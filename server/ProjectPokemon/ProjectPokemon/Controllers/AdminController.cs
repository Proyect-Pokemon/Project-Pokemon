using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.Admin;

namespace ProjectPokemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase {
    private readonly UnitOfWork _unitOfWork;

    public AdminController(UnitOfWork unitOfWork) {
        _unitOfWork = unitOfWork;
    }

    // GET: api/admin
    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IEnumerable<GetAdminUserDto>> GetAllUsers() {
        ICollection<User> users = await _unitOfWork.UserRepository.GetAllAsync();

        return users.Select(user => new GetAdminUserDto {
            Id = user.Id,
            Nickname = user.Nickname,
            Email = user.Email,
            Role = user.Role,
            AvatarPath = user.AvatarPath,
            CreationDate = user.CreationDate
        });
    }

    // PUT: api/admin/{id}Role
    [Authorize(Roles = "admin")]
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] PutUserRoleDto dto) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null) {
            return NotFound();
        }

        user.Role = dto.Role;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        bool success = await _unitOfWork.SaveAsync();

        if (!success) {
            return BadRequest();
        }

        return Ok(dto);
    }

    // DELETE: api/admin/{id}
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null) {
            return NotFound();
        }

        await _unitOfWork.UserRepository.DeleteAsync(user);
        bool success = await _unitOfWork.SaveAsync();

        if (!success) {
            return BadRequest();
        }

        return Ok();
    }
}