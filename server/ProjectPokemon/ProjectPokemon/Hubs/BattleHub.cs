using Microsoft.AspNetCore.SignalR;
using ProjectPokemon.Services;
using ProjectPokemon.Models.WebSocket;
using ProjectPokemon.Models.Battle;
using System.Text.Json;

namespace ProjectPokemon.Hubs;

// Hub de SignalR para comunicación en tiempo real de batallas
public class BattleHub : Hub {
    private readonly BattleSessionManager _sessionManager;
    private readonly ILogger<BattleHub> _logger;

    public BattleHub(BattleSessionManager sessionManager, ILogger<BattleHub> logger) {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public override Task OnConnectedAsync() {
        _logger.LogInformation($"Cliente conectado: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        _logger.LogInformation($"Cliente desconectado: {Context.ConnectionId}");

        // Limpiar batalla si el usuario se desconecta
        var battle = _sessionManager.GetBattleByConnectionId(Context.ConnectionId);
        if (battle != null) {
            _sessionManager.RemoveBattle(battle.BattleId);
            _logger.LogInformation($"Batalla {battle.BattleId} eliminada por desconexión");
        }

        return base.OnDisconnectedAsync(exception);
    }

    // El cliente envía una acción de batalla
    public async Task BattleAction(string actionJson) {
        try {
            var action = JsonSerializer.Deserialize<BattleActionMessage>(actionJson);
            if (action == null) {
                await SendError("Acción inválida");
                return;
            }

            var battle = _sessionManager.GetBattle(action.BattleId);
            if (battle == null) {
                await SendError("Batalla no encontrada");
                return;
            }

            // TODO: Procesar la acción (attack o switch)
            // Por ahora solo enviamos un mensaje de confirmación
            battle.BattleLog.Add($"Acción recibida: {action.Action}");

            await SendBattleUpdate(battle, new List<string> { $"Procesando acción: {action.Action}" });
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error procesando acción de batalla");
            await SendError("Error procesando acción");
        }
    }

    // Envía el estado actualizado de la batalla al cliente
    private async Task SendBattleUpdate(BattleSession battle, List<string>? messages = null) {
        var message = new WebSocketMessage {
            Type = "battle",
            Data = new BattleStateData {
                Battle = battle.CreateSnapshot(),
                Messages = messages ?? new List<string>(),
                RequiresSwitch = false, // TODO: Detectar si necesita cambio
                WinnerSide = battle.WinnerSide
            }
        };

        await Clients.Client(battle.PlayerConnectionId!).SendAsync("ReceiveMessage", 
            JsonSerializer.Serialize(message));
    }

    private async Task SendError(string errorMessage) {
        var message = new WebSocketMessage {
            Type = "error",
            Data = new { message = errorMessage }
        };

        await Clients.Caller.SendAsync("ReceiveMessage", JsonSerializer.Serialize(message));
    }
}