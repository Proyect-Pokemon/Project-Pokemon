using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Helpers;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.User;

namespace SocialNetwork.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase {
    // InyecciÃ³n de UserRepository 
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
            Password = user.Password
        });
        return getUsersDto;
    }


    // POST
    [HttpPost]
    public async Task<ActionResult<AddUserDto>> AddUser([FromBody] AddUserDto dto) {
        User user = new User {
            Email = dto.Email,
            Nickname = dto.Nickname,
            Password = PasswordHelper.Hash(dto.Password),
            AvatarPath = dto.AvatarPath,
            Biography = dto.Biography
        };

        await _unitOfWork.UserRepository.InsertAsync(user);
        bool success = await _unitOfWork.SaveAsync();

        if (!success) {
            return BadRequest();
        }

        return Ok(dto);
    }
}