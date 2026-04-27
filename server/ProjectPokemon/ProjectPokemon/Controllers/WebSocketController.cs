using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Networking;

namespace ProjectPokemon.Controllers;

[Route("[controller]")]
[ApiController]
public class WebSocketController : ControllerBase {
    private readonly Network _network;

    public WebSocketController(Network network) {
        _network = network;
    }

    [HttpGet]
    public async Task ConnectAsync() {
        if (HttpContext.WebSockets.IsWebSocketRequest) {
            WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _network.ConnectAsync(webSocket);
        }
        else {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
