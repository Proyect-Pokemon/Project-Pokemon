using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.User;
using SocialNetwork.Models.Dtos.Users;

namespace ProjectPokemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase {
    // Inyeccion de UserRepository 
    private readonly UnitOfWork _unitOfWork;

    public UsersController(UnitOfWork unitOfWork) {
        _unitOfWork = unitOfWork;
    }

    //GET: api/users
    [HttpGet]
    public async Task<IEnumerable<GetUserDto>> GetAllUsers() {
        ICollection<User> users = await _unitOfWork.UserRepository.GetAllAsync();

        IEnumerable<GetUserDto> getUsersDto = users.Select(user => new GetUserDto {
            Id = user.Id,
            Nickname = user.Nickname,
            AvatarPath = user.AvatarPath!
        });
        return getUsersDto;
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<GetUserProfileExtendDto> GetProfileUsers(int userId) {
        User? user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

        if (user == null) throw new Exception("User not found");

        var dto = new GetUserProfileExtendDto {
            Email = user.Email,
            Nickname = user.Nickname,
            Biography = user.Biography,
            AvatarPath = user.AvatarPath,
            FavoriteTeamId = user.FavoriteTeamId
        };
        return dto;
    }

    // PUT
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> UpdateFavoriteTeam(int id, [FromBody] PutFavoriteTeamDto dto) {

        User? user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null) {
            return NotFound();
        }

        user.FavoriteTeamId = dto.FavoriteTeamId;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) {
            return BadRequest();
        }
        return Ok(dto);
    }
}