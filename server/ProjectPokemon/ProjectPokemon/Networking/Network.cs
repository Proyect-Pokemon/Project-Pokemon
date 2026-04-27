using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Matchmaking _matchmaking;
    private readonly ILogger<Network> _logger;

    public Network(BattleSessionManager sessionManager, IServiceScopeFactory scopeFactory, ILogger<Network> logger) {
        _sessionManager = sessionManager;
        _scopeFactory = scopeFactory;
        _matchmaking = new Matchmaking();
        _logger = logger;

        // Suscribirse al evento de emparejamiento
        _matchmaking.Matched += OnPlayersMatched;
    }

    public Task ConnectAsync(WebSocket webSocket, int? userId = null, string? username = null) {
        Guid clientId = Guid.NewGuid();
        Client client = new Client(clientId, webSocket) {
            UserId = userId,
            Username = username
        };
        client.MessageReceived += OnClientMessageReceived;
        client.Disconnected += OnClientDisconnected;
        _clients.Add(clientId, client);

        _logger.LogInformation($"Cliente {clientId} conectado (userId={userId})");

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

                case SearchBattleRequest searchRequest:
                    await HandleSearchBattle(client, searchRequest);
                    break;

                case CancelSearchRequest cancelRequest:
                    await HandleCancelSearch(client, cancelRequest);
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
            int userId = client.UserId ?? 1;
            using IServiceScope scope = _scopeFactory.CreateScope();
            BattleService battleService = scope.ServiceProvider.GetRequiredService<BattleService>();

            _logger.LogInformation($"Cliente {client.ClientId} solicitó iniciar batalla con equipo {request.TeamId}");

            Models.Battle.BattleSession? session = await battleService.StartBattleAsync(userId, request.TeamId);
            if (session is null) {
                await client.SendAsync(new StartBattleResponse {
                    Action = BattleAction.StartBattle,
                    BattleId = string.Empty,
                    InitialState = new BattleSnapshot {
                        BattleId = string.Empty,
                        Turn = 0,
                        PlayerSide = new BattleSideSnapshot { Team = new(), ActiveSlot = 0 },
                        OpponentSide = new BattleSideSnapshot { Team = new(), ActiveSlot = 0 }
                    },
                    Success = false,
                    ErrorMessage = "No se pudo crear la batalla"
                });

                return;
            }

            var response = new StartBattleResponse {
                Action = BattleAction.StartBattle,
                BattleId = session.BattleId,
                InitialState = CreateBattleSnapshot(session),
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
            Username = client.Username ?? $"User_{client.UserId ?? 0}",
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
            SenderName = chatMessage.SenderName ?? client.Username ?? $"User_{client.UserId ?? 0}",
            Timestamp = DateTime.UtcNow
        };

        await BroadcastToBattleAsync(chatMessage.BattleId, response);
    }

    private void OnClientDisconnected(Client client) {
        client.MessageReceived -= OnClientMessageReceived;
        client.Disconnected -= OnClientDisconnected;
        _clients.Remove(client.ClientId);

        // Eliminar de la cola de matchmaking si estaba buscando
        _matchmaking.Leave(client);

        // Limpiar de todas las batallas
        foreach (var battleClients in _battleClients.Values) {
            battleClients.Remove(client.ClientId);
        }

        _logger.LogInformation($"Cliente {client.ClientId} desconectado");
    }

    // Handler para búsqueda de batalla
    private async Task HandleSearchBattle(Client client, SearchBattleRequest request) {
        _logger.LogInformation($"Cliente {client.ClientId} busca batalla con equipo {request.TeamId}");

        // Validar que el usuario tenga el equipo
        // TODO: Validar con base de datos que el equipo existe y pertenece al usuario

        // Unir a la cola de matchmaking
        _matchmaking.Join(client, request.TeamId);

        // Enviar respuesta de que está buscando
        var response = new SearchBattleResponse {
            Action = LobbyAction.SearchBattle,
            Success = true,
            Message = "Buscando rival..."
        };

        await client.SendAsync(response);
    }

    // Handler para cancelar búsqueda
    private async Task HandleCancelSearch(Client client, CancelSearchRequest request) {
        _logger.LogInformation($"Cliente {client.ClientId} canceló búsqueda de batalla");

        bool wasInQueue = _matchmaking.Leave(client);

        var response = new CancelSearchResponse {
            Action = LobbyAction.CancelSearch,
            Success = wasInQueue
        };

        await client.SendAsync(response);
    }

    // Evento que se dispara cuando dos jugadores son emparejados
    private async void OnPlayersMatched(Client player1, Client player2, int team1Id, int team2Id) {
        try {
            _logger.LogInformation($"Emparejamiento: {player1.ClientId} vs {player2.ClientId}");

            // Crear la batalla
            using IServiceScope scope = _scopeFactory.CreateScope();
            BattleService battleService = scope.ServiceProvider.GetRequiredService<BattleService>();

            var session = await battleService.StartBattleAsync(
                player1.UserId ?? 1, 
                team1Id
            );

            if (session == null) {
                _logger.LogError("Error al crear sesión de batalla");
                return;
            }

            // Unir ambos jugadores a la sala de batalla
            JoinBattle(player1.ClientId, session.BattleId);
            JoinBattle(player2.ClientId, session.BattleId);

            // Notificar a ambos jugadores que se encontró rival
            var notification1 = new BattleMatchedNotification {
                Action = LobbyAction.SearchBattle,
                BattleId = session.BattleId,
                OpponentUsername = player2.Username ?? $"User_{player2.UserId ?? 0}",
                OpponentUserId = player2.UserId ?? 0
            };

            var notification2 = new BattleMatchedNotification {
                Action = LobbyAction.SearchBattle,
                BattleId = session.BattleId,
                OpponentUsername = player1.Username ?? $"User_{player1.UserId ?? 0}",
                OpponentUserId = player1.UserId ?? 0
            };

            await player1.SendAsync(notification1);
            await player2.SendAsync(notification2);

            // Enviar estado inicial de la batalla a ambos
            var initialState = new BattleStateUpdate {
                Action = BattleAction.StartBattle,
                Battle = CreateBattleSnapshot(session),
                Messages = new List<string> { "¡La batalla comienza!" },
                RequiresSwitch = false,
                WinnerSide = null
            };

            await BroadcastToBattleAsync(session.BattleId, initialState);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error en emparejamiento de jugadores");
        }
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
