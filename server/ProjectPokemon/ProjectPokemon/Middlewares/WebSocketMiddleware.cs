using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Primitives;

namespace ProjectPokemon.Middlewares;

// Claves usadas para almacenar información en HttpContext.Items
// durante la conexión WebSocket.
public static class WebSocketContextKeys
{
    public const string User = "WS_USER";
}

// Extensión para acceder fácilmente al usuario autenticado
// desde cualquier parte del pipeline.
public static class WebSocketContextExtensions
{
    public static ClaimsPrincipal? GetUser(this HttpContext context)
        => context.Items.TryGetValue(WebSocketContextKeys.User, out var user)
            ? user as ClaimsPrincipal
            : null;
}

// Middleware encargado de autenticar conexiones WebSocket
// mediante JWT antes de permitir acceso.

public class WebSocketMiddleware : IMiddleware
{
    private const string TOKEN_KEY = "token";

    // Handler reutilizable para parsear y validar JWT
    private static readonly JwtSecurityTokenHandler _handler = new();

    // Algoritmos permitidos para evitar tokens manipulados o inseguros
    private static readonly HashSet<string> AllowedAlgs =
        new() { SecurityAlgorithms.HmacSha256 };

    private readonly TokenValidationParameters _validationParameters;

    public WebSocketMiddleware(TokenValidationParameters validationParameters)
    {
        _validationParameters = validationParameters;

        // Fail fast si la configuración JWT es incorrecta
        if (_validationParameters.ValidIssuer == null ||
            _validationParameters.ValidAudience == null)
        {
            throw new InvalidOperationException("JWT validation misconfigured");
        }
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Solo interceptamos peticiones WebSocket
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await next(context);
            return;
        }

        // Extraemos el token desde query o header
        string? token = ExtractToken(context);

        token = token?.Trim();

        // Validación básica de existencia
        if (string.IsNullOrWhiteSpace(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        try
        {
            // Validación completa del JWT (firma, expiración, issuer, audience)
            ClaimsPrincipal principal = _handler.ValidateToken(
                token,
                _validationParameters,
                out SecurityToken validatedToken
            );

            // Seguridad adicional: validación explícita del algoritmo
            if (validatedToken is JwtSecurityToken jwt)
            {
                if (string.IsNullOrWhiteSpace(jwt.Header.Alg) ||
                    !AllowedAlgs.Contains(jwt.Header.Alg))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            // Guardamos el usuario autenticado para el resto del pipeline
            context.Items[WebSocketContextKeys.User] = principal;
        }
        catch (SecurityTokenException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        catch (ArgumentException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Continuamos pipeline si todo es válido
        await next(context);
    }

    
    // Extrae el token JWT desde query string o headers.
    // Soporta:
    // - ?token=xxx
    // - Authorization: Bearer xxx

    private string? ExtractToken(HttpContext context)
    {
        // Query string
        if (context.Request.Query.TryGetValue(TOKEN_KEY, out var queryToken))
            return queryToken.ToString();

        // Header Authorization
        if (context.Request.Headers.TryGetValue("Authorization", out var headerToken))
        {
            var value = headerToken.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Formato Bearer token
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var extracted = value["Bearer ".Length..].Trim();
                return string.IsNullOrWhiteSpace(extracted) ? null : extracted;
            }

            return value;
        }

        return null;
    }
}