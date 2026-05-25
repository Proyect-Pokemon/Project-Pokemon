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
    private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _battleClients = new(); // battleId -> clientIds
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
        Client client = new(clientId, webSocket) {
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
    public void JoinBattle(Guid clientId, Guid battleId) {
        if (!_battleClients.ContainsKey(battleId)) {
            _battleClients[battleId] = new HashSet<Guid>();
        }
        _battleClients[battleId].Add(clientId);
        _logger.LogInformation($"Cliente {clientId} unido a batalla {battleId}");
    }

    // Envía un mensaje a todos los clientes de una batalla
    public async Task BroadcastToBattleAsync<T>(Guid battleId, T message) where T : IMessage {
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
        } catch (Exception ex) {
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
                    BattleId = Guid.Empty,
                    InitialState = new BattleSnapshot {
                        BattleId = Guid.Empty,
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
        } catch (Exception ex) {
            _logger.LogError(ex, "Error iniciando batalla");

            var errorResponse = new StartBattleResponse {
                Action = BattleAction.StartBattle,
                BattleId = Guid.Empty,
                InitialState = new BattleSnapshot {
                    BattleId = Guid.Empty,
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

        _logger.LogInformation(
            "Accion recibida en batalla {BattleId}: userId={UserId}, action={Action}, move={MoveName}, targetSlot={TargetSlot}",
            actionRequest.BattleId,
            client.UserId,
            actionRequest.Action,
            actionRequest.MoveName,
            actionRequest.TargetSlot
        );

        int? userId = client.UserId;
        if (!userId.HasValue) {
            await client.SendAsync(new BattleStateUpdate {
                Action = actionRequest.Action,
                Battle = CreateBattleSnapshot(battle),
                Messages = new List<string> { "No se pudo identificar al jugador." },
                RequiresSwitch = false,
                WinnerUserId = battle.WinnerUserId
            });
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        BattleService battleService = scope.ServiceProvider.GetRequiredService<BattleService>();

        BattleService.SubmitBattleActionResult result = battleService.SubmitPvPAction(
            battle,
            userId.Value,
            actionRequest.Action,
            actionRequest.MoveName,
            actionRequest.TargetSlot
        );

        if (!result.Accepted) {
            _logger.LogWarning(
                "Accion rechazada en batalla {BattleId} para userId={UserId}. Motivo: {Messages}",
                actionRequest.BattleId,
                userId.Value,
                string.Join(" | ", result.Messages)
            );

            await client.SendAsync(new BattleStateUpdate {
                Action = actionRequest.Action,
                Battle = CreateBattleSnapshot(battle, userId.Value),
                Messages = result.Messages,
                RequiresSwitch = false,
                WinnerUserId = battle.WinnerUserId
            });
            return;
        }

        // Si sólo uno eligió acción, notificar sólo al emisor.
        if (!result.TurnResolved) {
            _logger.LogInformation(
                "Batalla {BattleId}: accion guardada para userId={UserId}, esperando rival.",
                actionRequest.BattleId,
                userId.Value
            );

            await client.SendAsync(new BattleStateUpdate {
                Action = actionRequest.Action,
                Battle = CreateBattleSnapshot(battle, userId.Value),
                Messages = result.Messages,
                RequiresSwitch = false,
                WinnerUserId = battle.WinnerUserId
            });
            return;
        }

        // Turno resuelto: enviar actualización personalizada a todos.
        _logger.LogInformation(
            "Batalla {BattleId}: turno resuelto. winner={Winner}. Logs={MessagesCount}",
            actionRequest.BattleId,
            result.WinnerUserId,
            result.Messages.Count
        );

        battle.BattleLog.AddRange(result.Messages);

        // Enviar actualización personalizada a cada jugador de la batalla
        if (_battleClients.TryGetValue(actionRequest.BattleId, out var clientIds)) {
            var tasks = clientIds
                .Where(id => _clients.ContainsKey(id))
                .Select(id => {
                    int perspectiveUserId = _clients[id].UserId ?? battle.PlayerUserId;
                    var update = new BattleStateUpdate {
                        Action = actionRequest.Action,
                        Battle = CreateBattleSnapshot(battle, perspectiveUserId),
                        Messages = result.Messages,
                        RequiresSwitch = false,
                        WinnerUserId = battle.WinnerUserId
                    };
                    return _clients[id].SendAsync(update);
                });

            await Task.WhenAll(tasks);
        }

        if (battle.WinnerUserId.HasValue) {
            CleanupFinishedBattle(actionRequest.BattleId);
        }
    }

    private void CleanupFinishedBattle(Guid battleId) {
        _battleClients.TryRemove(battleId, out _);
        _sessionManager.RemoveBattle(battleId);
        _logger.LogInformation("Batalla {BattleId} finalizada y limpiada de suscripciones activas.", battleId);
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

            // Crear la batalla PvP con los equipos reales de ambos jugadores
            using IServiceScope scope = _scopeFactory.CreateScope();
            BattleService battleService = scope.ServiceProvider.GetRequiredService<BattleService>();

            var session = await battleService.StartPvPBattleAsync(
                player1.UserId ?? 1,
                team1Id,
                player2.UserId ?? 2,
                team2Id
            );

            if (session == null) {
                _logger.LogError("Error al crear sesión de batalla PvP");
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

            // Enviar estado inicial personalizado: cada jugador ve sus pokemon como "playerSide"
            var stateForPlayer1 = new BattleStateUpdate {
                Action = BattleAction.StartBattle,
                Battle = CreateBattleSnapshot(session, player1.UserId ?? 1),
                Messages = new List<string> { "¡La batalla comienza!" },
                RequiresSwitch = false,
                WinnerUserId = null
            };

            var stateForPlayer2 = new BattleStateUpdate {
                Action = BattleAction.StartBattle,
                Battle = CreateBattleSnapshot(session, player2.UserId ?? 2),
                Messages = new List<string> { "¡La batalla comienza!" },
                RequiresSwitch = false,
                WinnerUserId = null
            };

            await player1.SendAsync(stateForPlayer1);
            await player2.SendAsync(stateForPlayer2);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error en emparejamiento de jugadores");
        }
    }

    // Snapshot desde la perspectiva de un jugador concreto (sus Pokémon en playerSide)
    private BattleSnapshot CreateBattleSnapshot(Models.Battle.BattleSession battle, int perspectiveUserId) {
        // Player1 siempre es PlayerSide, Player2 es OpponentSide
        bool isPlayer1 = battle.PlayerUserId == perspectiveUserId;

        var mySide = isPlayer1 ? battle.PlayerSide : battle.OpponentSide;
        var theirSide = isPlayer1 ? battle.OpponentSide : battle.PlayerSide;

        return new BattleSnapshot {
            BattleId = battle.BattleId,
            Turn = battle.Turn,
            PlayerSide = new BattleSideSnapshot {
                Team = mySide.Team.Select(p => CreatePokemonSnapshot(p, true)).ToList(),
                ActiveSlot = mySide.ActiveSlot
            },
            OpponentSide = new BattleSideSnapshot {
                Team = theirSide.Team.Select(p => CreatePokemonSnapshot(p, false)).ToList(),
                ActiveSlot = theirSide.ActiveSlot
            }
        };
    }

    // Overload sin perspectiva (para modo CPU, player1 siempre es el jugador)
    private BattleSnapshot CreateBattleSnapshot(Models.Battle.BattleSession battle) {
        return CreateBattleSnapshot(battle, battle.PlayerUserId);
    }

    private PokemonSnapshot CreatePokemonSnapshot(Models.Battle.PokemonBattle pokemon, bool isPlayerSide) {
        // Obtener los estados secundarios activos como lista de strings
        var secondaryStatuses = new List<string>();
        foreach (Enum.PokeSecondaryStatus status in System.Enum.GetValues(typeof(Enum.PokeSecondaryStatus))) {
            if (status != Enum.PokeSecondaryStatus.None && pokemon.HasSecondaryStatus(status)) {
                secondaryStatuses.Add(status.ToString());
            }
        }

        return new PokemonSnapshot {
            PokemonId = pokemon.PokemonId,
            Name = pokemon.Name,
            Nickname = pokemon.Nickname,
            Sex = pokemon.Sex?.ToString(),
            Slot = pokemon.Slot,
            CurrentHp = pokemon.CurrentHp,
            MaxHp = pokemon.MaxHp,
            IsFainted = pokemon.IsFainted(),
            Status = pokemon.Status.ToString(),
            SecondaryStatuses = secondaryStatuses,
            SpriteFront = pokemon.SpriteFront,
            SpriteBack = pokemon.SpriteBack,
            SpriteFrontShiny = pokemon.SpriteFrontShiny,
            SpriteBackShiny = pokemon.SpriteBackShiny,
            SpriteFrontFem = pokemon.SpriteFrontFem,
            SpriteBackFem = pokemon.SpriteBackFem,
            SpriteFrontFemShiny = pokemon.SpriteFrontFemShiny,
            SpriteBackFemShiny = pokemon.SpriteBackFemShiny,
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