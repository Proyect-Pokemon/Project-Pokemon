using Google.Apis.Auth;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Services.Auth;

/// <summary>
/// Gestiona autenticación mediante Google OAuth
/// y creación automática de usuarios.
/// </summary>
public sealed class GoogleAuthService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly TokenService _tokenService;
    private readonly string _googleClientId;

    public GoogleAuthService(
     UnitOfWork unitOfWork,
     TokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;

        _googleClientId = "dummy-google-client-id.apps.googleusercontent.com";
    }

    /// <summary>
    /// Valida token Google,
    /// obtiene o crea usuario
    /// y devuelve JWT propio.
    /// </summary>
    public async Task<string?> LoginWithGoogleAsync(
        string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload =
                await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience =
                            [
                                _googleClientId
                            ]
                    });
        }
        catch (Exception ex)
        {
            throw new Exception("Google token validation failed", ex);
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new Exception("Google payload missing email");
        }

        User? user =
            await _unitOfWork
                .UserRepository
                .GetUserByEmailAsync(
                    payload.Email
                );

        if (user is null)
        {
            user =
                await CreateGoogleUserAsync(
                    payload
                );
        }

        return _tokenService.CreateToken(
            user
        );
    }

    /// <summary>
    /// Crea usuario nuevo
    /// usando datos Google.
    /// </summary>
    private async Task<User>
    CreateGoogleUserAsync(
        GoogleJsonWebSignature.Payload payload)
    {
        var user = new User
        {
            Email = payload.Email!,
            Nickname = GenerateNickname(payload.Email, payload.Name),

            Password = string.Empty,
            Biography = string.Empty,

            Role = "user",
            AvatarPath = payload.Picture ?? "/assets/avatar-default.png",

            CreationDate = DateTime.UtcNow
        };

        await _unitOfWork
            .UserRepository
            .InsertAsync(user);

        await _unitOfWork.SaveAsync();

        return user;
    }

    /// <summary>
    /// Genera nickname básico
    /// desde nombre y correo.
    /// </summary>
    private static string GenerateNickname(
        string email,
        string? name)
    {
        string safeName =
            string.IsNullOrWhiteSpace(name)
            ? "User"
            : string.Concat(
                name.Where(
                    c => !char.IsWhiteSpace(c)
                ));

        string emailPrefix =
            email.Split('@')[0];

        return $"{safeName}_{emailPrefix}";
    }
}