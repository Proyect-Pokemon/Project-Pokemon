using System.Collections.Concurrent;
using System.Net.WebSockets;
using ProjectPokemon.Networking.Clients;
using ProjectPokemon.Networking.Messages;
using ProjectPokemon.Networking.Messages.Battle;
using ProjectPokemon.Networking.Messages.Lobby;
using ProjectPokemon.Services;

namespace ProjectPokemon.Networking;

// Gestiona todas las conexiones WebSocket y el enrutamiento de mensajes.
// Actúa como un "router" central que distribuye mensajes según su tipo.
public class Network {
    private readonly IDictionary<Guid, Client> _clients = new ConcurrentDictionary<Guid, Client>();
    private readonly IDictionary<string, HashSet<Guid>> _battleClients = new ConcurrentDictionary<string, HashSet<Guid>>(); // battleId -> clientIds
    private readonly BattleSessionManager _sessionManager;
    private readonly ILogger<Network> _logger;

    public Network(BattleSessionManager sessionManager, ILogger<Network> logger) {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public Task ConnectAsync(WebSocket webSocket) {
        Guid clientId = Guid.NewGuid();
        Client client = new Client(clientId, webSocket);
        client.MessageReceived += OnClientMessageReceived;
        client.Disconnected += OnClientDisconnected;
        _clients.Add(clientId, client);

        _logger.LogInformation($"Cliente {clientId} conectado");

        return client.KeepListenAsync();
    }

    // Asocia un cliente con una batalla específica
    public void JoinBattle(Guid clientId, string battleId) {
        if (!_battleClients.ContainsKey(battleId)) {
            _battleClients[battleId] = new HashSet<Guid>();
        }
        _battleClients[battleId].Add(clientId);
        _logger.LogInformation($"Cliente {clientId} unido a batalla {battleId}");
    }

    // Envía un mensaje a todos los clientes de una batalla
    public async Task BroadcastToBattleAsync<T>(string battleId, T message) where T : IMessage {
        if (!_battleClients.TryGetValue(battleId, out var clientIds)) {
            return;
        }

        var tasks = clientIds
            .Where(id => _clients.ContainsKey(id))
            .Select(id => _clients[id].SendAsync(message));

        await Task.WhenAll(tasks);
    }

    private async void OnClientMessageReceived(Client client, IMessage message) {
        try {
            switch (message) {
                case StartBattleRequest startRequest:
                    await HandleStartBattle(client, startRequest);
                    break;

                case BattleActionRequest actionRequest:
                    await HandleBattleAction(client, actionRequest);
                    break;

                case ChatMessage chatMessage:
                    await HandleChatMessage(client, chatMessage);
                    break;

                case JoinLobbyRequest joinLobby:
                    await HandleJoinLobby(client, joinLobby);
                    break;

                default:
                    _logger.LogWarning($"Mensaje no reconocido de cliente {client.ClientId}");
                    break;
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"Error procesando mensaje de cliente {client.ClientId}");
        }
    }

    private async Task HandleStartBattle(Client client, StartBattleRequest request) {
        try {
            // TODO: Obtener userId real del cliente autenticado
            int userId = client.UserId ?? 1; // Placeholder

            // Crear batalla usando BattleService
            var battleService = new BattleService(
                null!, // TODO: Inyectar dependencias correctamente
                _sessionManager,
                _logger as ILogger<BattleService>
            );

            // TODO: Por ahora crear batalla solo necesita userId y teamId
            // En el futuro: soportar PvP cuando request.OpponentUserId != null

            _logger.LogInformation($"Cliente {client.ClientId} solicitó iniciar batalla con equipo {request.TeamId}");

            // Por ahora, enviar respuesta exitosa sin crear batalla real (TODO)
            var response = new StartBattleResponse {
                Action = BattleAction.StartBattle,
                BattleId = Guid.NewGuid().ToString(),
                InitialState = new BattleSnapshot {
                    BattleId = Guid.NewGuid().ToString(),
                    Turn = 1,
                    PlayerSide = new BattleSideSnapshot { Team = new(), ActiveSlot = 0 },
                    OpponentSide = new BattleSideSnapshot { Team = new(), ActiveSlot = 0 }
                },
                Success = true
            };

            await client.SendAsync(response);

            // Unir al cliente a la batalla
            JoinBattle(client.ClientId, response.BattleId);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error iniciando batalla");

            var errorResponse = new StartBattleResponse {
                Action = BattleAction.StartBattle,
                BattleId = "",
                InitialState = new BattleSnapshot {
                    BattleId = "",
                    Turn = 0,
                    PlayerSide = new BattleSideSnapshot { Team = new(), ActiveSlot = 0 },
                    OpponentSide = new BattleSideSnapshot { Team = new(), ActiveSlot = 0 }
                },
                Success = false,
                ErrorMessage = ex.Message
            };

            await client.SendAsync(errorResponse);
        }
    }

    private async Task HandleJoinLobby(Client client, JoinLobbyRequest request) {
        _logger.LogInformation($"Cliente {client.ClientId} se unió al lobby");

        // TODO: Obtener lista real de amigos online
        var response = new JoinLobbyResponse {
            Action = LobbyAction.JoinLobby,
            Username = $"User_{client.UserId ?? 0}",
            OnlineFriends = new List<OnlineFriend>()
        };

        await client.SendAsync(response);
    }

    private async Task HandleBattleAction(Client client, BattleActionRequest actionRequest) {
        var battle = _sessionManager.GetBattle(actionRequest.BattleId);
        if (battle == null) {
            _logger.LogWarning($"Batalla {actionRequest.BattleId} no encontrada");
            return;
        }

        // TODO: Procesar la acción (attack, switch, etc.)
        battle.BattleLog.Add($"Acción recibida: {actionRequest.Action}");

        // Enviar actualización de estado a todos los clientes de la batalla
        var update = new BattleStateUpdate {
            Action = actionRequest.Action,
            Battle = CreateBattleSnapshot(battle),
            Messages = new List<string> { $"Procesando acción: {actionRequest.Action}" },
            RequiresSwitch = false,
            WinnerSide = battle.WinnerSide
        };

        await BroadcastToBattleAsync(actionRequest.BattleId, update);
    }

    private async Task HandleChatMessage(Client client, ChatMessage chatMessage) {
        var battle = _sessionManager.GetBattle(chatMessage.BattleId);
        if (battle == null) {
            _logger.LogWarning($"Batalla {chatMessage.BattleId} no encontrada para chat");
            return;
        }

        // Broadcast mensaje de chat a todos los clientes de la batalla
        var response = new ChatMessageReceived {
            BattleId = chatMessage.BattleId,
            Content = chatMessage.Content,
            SenderName = chatMessage.SenderName ?? "Jugador",
            Timestamp = DateTime.UtcNow
        };

        await BroadcastToBattleAsync(chatMessage.BattleId, response);
    }

    private void OnClientDisconnected(Client client) {
        client.MessageReceived -= OnClientMessageReceived;
        client.Disconnected -= OnClientDisconnected;
        _clients.Remove(client.ClientId);

        // Limpiar de todas las batallas
        foreach (var battleClients in _battleClients.Values) {
            battleClients.Remove(client.ClientId);
        }

        _logger.LogInformation($"Cliente {client.ClientId} desconectado");
    }

    private BattleSnapshot CreateBattleSnapshot(Models.Battle.BattleSession battle) {
        return new BattleSnapshot {
            BattleId = battle.BattleId,
            Turn = battle.Turn,
            PlayerSide = new BattleSideSnapshot {
                Team = battle.PlayerSide.Team.Select(p => CreatePokemonSnapshot(p, true)).ToList(),
                ActiveSlot = battle.PlayerSide.ActiveSlot
            },
            OpponentSide = new BattleSideSnapshot {
                Team = battle.OpponentSide.Team.Select(p => CreatePokemonSnapshot(p, false)).ToList(),
                ActiveSlot = battle.OpponentSide.ActiveSlot
            }
        };
    }

    private PokemonSnapshot CreatePokemonSnapshot(Models.Battle.PokemonBattle pokemon, bool isPlayerSide) {
        return new PokemonSnapshot {
            PokemonId = pokemon.PokemonId,
            Name = pokemon.Name,
            Nickname = pokemon.Nickname,
            Slot = pokemon.Slot,
            CurrentHp = pokemon.CurrentHp,
            MaxHp = pokemon.MaxHp,
            IsFainted = pokemon.IsFainted(),
            Status = pokemon.Status.ToString(),
            SpriteFront = null, // TODO: Obtener del Pokemon entity
            SpriteBack = null,
            Shiny = pokemon.Shiny,
            Movements = pokemon.Movements.Select(m => new MovementSnapshot {
                Name = m.Name,
                CurrentPp = m.CurrentPp,
                MaxPp = m.MaxPp,
                Type = m.Type.ToString()
            }).ToList(),
            Attack = pokemon.GetModifiedStat(Enum.StatType.Attack),
            Defense = pokemon.GetModifiedStat(Enum.StatType.Defense),
            SpecialAttack = pokemon.GetModifiedStat(Enum.StatType.SpecialAttack),
            SpecialDefense = pokemon.GetModifiedStat(Enum.StatType.SpecialDefense),
            Speed = pokemon.GetModifiedStat(Enum.StatType.Speed)
        };
    }
}
