namespace ProjectPokemon.Networking.Messages.Battle;

// Mensaje de chat durante la batalla
public class ChatMessage : IMessage {
    public MessageType Type => MessageType.Chat;
    public required string BattleId { get; set; }
    public required string Content { get; set; }
    public string? SenderName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Respuesta del servidor con el mensaje de chat
public class ChatMessageReceived : IMessage {
    public MessageType Type => MessageType.Chat;
    public required string BattleId { get; set; }
    public required string Content { get; set; }
    public required string SenderName { get; set; }
    public DateTime Timestamp { get; set; }
}
