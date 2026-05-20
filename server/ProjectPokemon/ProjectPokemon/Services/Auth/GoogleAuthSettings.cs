namespace ProjectPokemon.Services.Auth
{
    /// <summary>
    /// Configuración de autenticación Google OAuth.
    /// Se rellena desde appsettings.json.
    /// </summary>
    public sealed class GoogleAuthSettings
    {
        /// <summary>
        /// Client ID generado en Google Cloud Console.
        /// </summary>
        public string ClientId { get; init; } = string.Empty;
    }
}