using ProjectPokemon.Enum;
using ProjectPokemon.Helpers;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.Auth;
using ProjectPokemon.Models.Dtos.User;

namespace ProjectPokemon.Services.Auth;

public class AuthService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly TokenService _tokenService;

    public AuthService(UnitOfWork unitOfWork, TokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<string?> LoginAsync(LoginModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Nickname) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            return null;
        }

        User? user =
            await _unitOfWork.UserRepository.GetUserByNicknameAsync(model.Nickname);

        if (user is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.Password))
        {
            return null;
        }

        if (!PasswordHelper.Verify(model.Password, user.Password))
        {
            return null;
        }

        return _tokenService.CreateToken(user);
    }

    public async Task<string?> CheckUserExists(string email, string nickname)
    {
        User? user =
            await _unitOfWork.UserRepository.GetUserByNicknameAsync(nickname);

        if (user != null)
        {
            return "Existe el usuario con ese nickname";
        }

        user =
            await _unitOfWork.UserRepository.GetUserByEmailAsync(email);

        if (user != null)
        {
            return "Existe el usuario con ese email";
        }

        return null;
    }

    public async Task<RegisterResult> RegisterAsync(AddUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Nickname) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return RegisterResult.InvalidData;
        }

        User? existingNick =
            await _unitOfWork.UserRepository.GetUserByNicknameAsync(dto.Nickname);

        if (existingNick != null)
        {
            return RegisterResult.NicknameAlreadyExists;
        }

        User? existingEmail =
            await _unitOfWork.UserRepository.GetUserByEmailAsync(dto.Email);

        if (existingEmail != null)
        {
            return RegisterResult.EmailAlreadyExists;
        }

        var userEntity = new User
        {
            Email = dto.Email,
            Nickname = dto.Nickname,
            AvatarPath = dto.AvatarPath,
            Password = PasswordHelper.Hash(dto.Password),
            Biography = dto.Biography,
            Role = "user"
        };

        await _unitOfWork.UserRepository.InsertAsync(userEntity);
        await _unitOfWork.SaveAsync();

        return RegisterResult.Success;
    }
}