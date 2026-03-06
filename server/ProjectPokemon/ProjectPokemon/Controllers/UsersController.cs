using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.User;

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
}