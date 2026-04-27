namespace ProjectPokemon.Networking.Messages.Lobby;

// Mensaje base para lobby
public abstract class LobbyMessage : IMessage<LobbyAction> {
    public MessageType Type => MessageType.Lobby;
    public required LobbyAction Action { get; set; }
}

// Cliente solicita unirse al lobby (al conectarse)
public class JoinLobbyRequest : LobbyMessage {
    // Automático al conectar, no necesita datos adicionales
}

// Respuesta del servidor al unirse al lobby
public class JoinLobbyResponse : LobbyMessage {
    public required string Username { get; set; }
    public List<OnlineFriend> OnlineFriends { get; set; } = new();
}

// Representa un amigo online
public class OnlineFriend {
    public required int UserId { get; set; }
    public required string Username { get; set; }
    public required string Status { get; set; } // "online", "in_battle", "away"
}

// Cliente solicita buscar combate
public class SearchBattleRequest : LobbyMessage {
    public required int TeamId { get; set; }
}

// Servidor notifica que está buscando combate
public class SearchBattleResponse : LobbyMessage {
    public bool Success { get; set; }
    public string? Message { get; set; }
}

// Servidor notifica que se canceló la búsqueda
public class CancelSearchRequest : LobbyMessage {
    // No necesita datos adicionales
}

// Respuesta al cancelar búsqueda
public class CancelSearchResponse : LobbyMessage {
    public bool Success { get; set; }
}

// Servidor notifica que se encontró un rival y empieza la batalla
public class BattleMatchedNotification : LobbyMessage {
    public required string BattleId { get; set; }
    public required string OpponentUsername { get; set; }
    public int OpponentUserId { get; set; }
}
