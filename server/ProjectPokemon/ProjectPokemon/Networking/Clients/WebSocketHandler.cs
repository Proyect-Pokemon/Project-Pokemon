using System.Net.WebSockets;
using System.Text;

namespace ProjectPokemon.Networking.Clients;

public class WebSocketHandler : IDisposable {
    private readonly WebSocket _socket;

    public bool IsOpen => _socket.State == WebSocketState.Open;

    public WebSocketHandler(WebSocket socket) {
        _socket = socket;
    }

    public async Task SendAsync(string message, CancellationToken cancellation = default) {
        // Si el websocket está abierto, enviamos el mensaje
        if (IsOpen) {
            // Enviamos el mensaje
            using Stream writableWebSocketStream = WebSocketStream.CreateWritableMessageStream(_socket, WebSocketMessageType.Text);
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await writableWebSocketStream.WriteAsync(bytes, cancellation);
        }
    }

    public async Task<string> ReadAsync(CancellationToken cancellation = default) {
        // Creamos streams para leer el mensaje
        using Stream readableWebSocketStream = WebSocketStream.CreateReadableMessageStream(_socket);
        using StreamReader reader = new StreamReader(readableWebSocketStream, Encoding.UTF8);

        // Leemos el mensaje
        string message = await reader.ReadToEndAsync(cancellation);

        return message;
    }

    public void Dispose() {
        _socket.Dispose();
    }
}
