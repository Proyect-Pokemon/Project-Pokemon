using System.Net.WebSockets;
using ProjectPokemon.Networking.Messages;

namespace ProjectPokemon.Networking.Clients;

// Representa un cliente conectado por WebSocket.
// Puede participar en batallas, chat, lobby, etc.
public class Client {
    private readonly WebSocketHandler _socketHandler;

    public Guid ClientId { get; }
    public int? UserId { get; set; } // Se asigna después de autenticación
    public string? Username { get; set; }

    // Eventos para notificar cuando se recibe un mensaje o se desconecta
    public event Action<Client, IMessage>? MessageReceived;
    public event Action<Client>? Disconnected;

    public Client(Guid id, WebSocket webSocket) {
        ClientId = id;
        _socketHandler = new WebSocketHandler(webSocket);
    }

    public Task SendAsync<T>(T message, CancellationToken cancellation = default) where T : IMessage {
        string json = MessageSerializer.Serialize(message);
        return _socketHandler.SendAsync(json, cancellation);
    }

    public async Task KeepListenAsync() {
        string? jsonMessage = null;

        while (_socketHandler.IsOpen) {
            try {
                jsonMessage = await _socketHandler.ReadAsync();

                if (!string.IsNullOrWhiteSpace(jsonMessage)) {
                    IMessage? message = MessageSerializer.Deserialize(jsonMessage);

                    if (message is not null)
                        MessageReceived?.Invoke(this, message);
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely) {
                Console.Error.WriteLine(ex);
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine(jsonMessage);
            }
        }

        Disconnected?.Invoke(this);
    }
}
