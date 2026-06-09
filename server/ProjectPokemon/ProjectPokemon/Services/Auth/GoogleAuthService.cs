using Google.Apis.Auth;
using Microsoft.Extensions.Options;
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
    private readonly GoogleAuthSettings _settings;

    public GoogleAuthService(
        UnitOfWork unitOfWork,
        TokenService tokenService,
        IOptions<GoogleAuthSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;

        _settings =
            settings.Value
            ?? throw new ArgumentNullException(
                nameof(settings)
            );

        ArgumentException.ThrowIfNullOrWhiteSpace(
            _settings.ClientId
        );
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
                            _settings.ClientId
                        ]
                    });
        }
        catch (InvalidJwtException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return null;
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
        var user =
            new User
            {
                Email = payload.Email!,

                Nickname =
                    GenerateNickname(
                        payload.Email,
                        payload.Name
                    ),

                Password = null,
                AvatarPath = payload.Picture,
                Biography = null,
                Role = "user"
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