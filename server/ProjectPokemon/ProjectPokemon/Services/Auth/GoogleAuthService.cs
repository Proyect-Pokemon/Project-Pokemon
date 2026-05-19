using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Services.Auth;

public class GoogleAuthService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly TokenService _tokenService;
    private readonly GoogleAuthSettings _settings;

    public GoogleAuthService(
        UnitOfWork unitOfWork,
        TokenService tokenService,
        IOptions<GoogleAuthSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _settings = settings.Value;
    }

    public async Task<string?> LoginWithGoogleAsync(string idToken)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _settings.ClientId }
                });
        }
        catch
        {
            return null; // token inválido
        }

        // 1. buscar usuario por email
        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(payload.Email);

        // 2. si no existe, crearlo
        if (user is null)
        {
            user = new User
            {
                Email = payload.Email,
                Nickname = GenerateNickname(payload.Email, payload.Name),
                Password = null,
                AvatarPath = payload.Picture,
                Role = "user",
                Biography = null
            };

            await _unitOfWork.UserRepository.InsertAsync(user);
            await _unitOfWork.SaveAsync();
        }

        // 3. generar JWT normal
        return _tokenService.CreateToken(user);
    }

    private string GenerateNickname(string email, string name)
    {
        return name.Replace(" ", "") + "_" + email.Split('@')[0];
    }
}