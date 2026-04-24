namespace ProjectPokemon.Models.WebSocket;

// Mensaje base para comunicación WebSocket
public class WebSocketMessage {
    public required string Type { get; set; }
    public object? Data { get; set; }
}