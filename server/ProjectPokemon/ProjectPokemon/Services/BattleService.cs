using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Battle;
using ProjectPokemon.Networking.Messages.Battle;

namespace ProjectPokemon.Services;

// Servicio para crear y gestionar batallas
public class BattleService {
    private readonly PokemonDbContext _context;
    private readonly BattleSessionManager _sessionManager;
    private readonly ILogger<BattleService> _logger;

    public BattleService(
        PokemonDbContext context,
        BattleSessionManager sessionManager,
        ILogger<BattleService> logger) {
        _context = context;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public class SubmitBattleActionResult {
        public bool Accepted { get; set; }
        public bool TurnResolved { get; set; }
        public List<string> Messages { get; set; } = new();
        public List<Networking.Messages.Battle.BattleEvent> Timeline { get; set; } = new();
        public int? WinnerUserId { get; set; }
    }

    // Crea una nueva batalla cargando el equipo del usuario (vs CPU con placeholder)
    public async Task<BattleSession?> StartBattleAsync(int userId, int teamId) {
        var playerTeam = await LoadTeamAsync(userId, teamId);
        if (playerTeam == null) return null;

        // Equipo rival temporal (placeholder) para modo CPU
        var opponentPokemonEntity = await _context.PokemonTeams
            .Include(pt => pt.Pokemon)
            .Include(pt => pt.Nature)
            .Include(pt => pt.Movement1)
                .ThenInclude(m => m!.StatChanges)
            .Include(pt => pt.Movement2)
                .ThenInclude(m => m!.StatChanges)
            .Include(pt => pt.Movement3)
                .ThenInclude(m => m!.StatChanges)
            .Include(pt => pt.Movement4)
                .ThenInclude(m => m!.StatChanges)
            .FirstOrDefaultAsync(pt => pt.Id == 1);

        if (opponentPokemonEntity == null) {
            _logger.LogError("No se encontró Pokémon rival placeholder");
            return null;
        }

        var opponentTeam = Enumerable.Range(0, 6)
            .Select(_ => new PokemonBattle(opponentPokemonEntity))
            .ToList();

        var session = new BattleSession {
            PlayerUserId = userId,
            PlayerSide = new BattleSide { Team = playerTeam, ActiveSlot = 0 },
            OpponentSide = new BattleSide { Team = opponentTeam, ActiveSlot = 0 }
        };

        _sessionManager.CreateBattle(session);
        _logger.LogInformation($"Batalla CPU {session.BattleId} creada para usuario {userId}");
        return session;
    }

    // Crea una batalla PvP cargando los equipos reales de ambos jugadores
    public async Task<BattleSession?> StartPvPBattleAsync(int player1UserId, int team1Id, int player2UserId, int team2Id) {
        var team1 = await LoadTeamAsync(player1UserId, team1Id);
        if (team1 == null) {
            _logger.LogWarning($"Equipo {team1Id} no encontrado para usuario {player1UserId}");
            return null;
        }

        var team2 = await LoadTeamAsync(player2UserId, team2Id);
        if (team2 == null) {
            _logger.LogWarning($"Equipo {team2Id} no encontrado para usuario {player2UserId}");
            return null;
        }

        var session = new BattleSession {
            PlayerUserId = player1UserId,
            Player2UserId = player2UserId,
            PlayerSide = new BattleSide { Team = team1, ActiveSlot = 0 },
            OpponentSide = new BattleSide { Team = team2, ActiveSlot = 0 }
        };

        _sessionManager.CreateBattle(session);
        _logger.LogInformation($"Batalla PvP {session.BattleId} creada: usuario {player1UserId} vs {player2UserId}");
        return session;
    }

    // Carga el equipo de un usuario desde la BD
    private async Task<List<PokemonBattle>?> LoadTeamAsync(int userId, int teamId) {
        var team = await _context.Teams
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Pokemon)
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Nature)
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Movement1)
                    .ThenInclude(m => m!.StatChanges)
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Movement2)
                    .ThenInclude(m => m!.StatChanges)
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Movement3)
                    .ThenInclude(m => m!.StatChanges)
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Movement4)
                    .ThenInclude(m => m!.StatChanges)
            .FirstOrDefaultAsync(t => t.Id == teamId && t.UserId == userId);

        if (team == null) {
            _logger.LogWarning($"Equipo {teamId} no encontrado o no pertenece al usuario {userId}");
            return null;
        }

        // Validar que tenga al menos 1 Pokémon
        if (team.PokemonsTeam.Count == 0) {
            _logger.LogWarning($"El equipo {teamId} está vacío");
            return null;
        }

        // Retornar los Pokémon del equipo tal como están (sin placeholders)
        return team.PokemonsTeam
            .OrderBy(pt => pt.Slot)
            .Select(pt => new PokemonBattle(pt))
            .ToList();
    }

    // Recibe la acción de un jugador y resuelve el turno cuando ambos han elegido
    public SubmitBattleActionResult SubmitPvPAction(
        BattleSession battle,
        int userId,
        BattleAction action,
        string? moveName,
        int? targetSlot) {

        lock (battle.SyncRoot) {
            if (!battle.IsParticipant(userId)) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    Messages = new List<string> { "No perteneces a esta batalla." },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (battle.WinnerUserId != null) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    Messages = new List<string> { "La batalla ya ha terminado." },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (action == BattleAction.Forfeit) {
                int? forfeitOpponentUserId = battle.GetOpponentUserId(userId);
                if (!forfeitOpponentUserId.HasValue) {
                    return new SubmitBattleActionResult {
                        Accepted = false,
                        TurnResolved = false,
                        Messages = new List<string> { "No hay rival asignado." },
                        WinnerUserId = battle.WinnerUserId
                    };
                }

                battle.WinnerUserId = forfeitOpponentUserId.Value;
                battle.Status = ProjectPokemon.Enum.BattleStatus.Finished;
                battle.PendingActionsByUserId.Clear();

                return new SubmitBattleActionResult {
                    Accepted = true,
                    TurnResolved = true,
                    Messages = new List<string> { "Un jugador se rindió." },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (!IsValidActionPayload(battle, userId, action, moveName, targetSlot, out string validationError)) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    Messages = new List<string> { validationError },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            battle.PendingActionsByUserId[userId] = new PendingBattleAction {
                Action = action,
                MoveName = moveName,
                TargetSlot = targetSlot
            };

            int? opponentUserId = battle.GetOpponentUserId(userId);
            if (!opponentUserId.HasValue) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    Messages = new List<string> { "No hay rival asignado." },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (!battle.PendingActionsByUserId.ContainsKey(opponentUserId.Value)) {
                return new SubmitBattleActionResult {
                    Accepted = true,
                    TurnResolved = false,
                    Messages = new List<string> { "Acción recibida. Esperando al rival..." },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            // Ya hay 2 acciones pendientes: resolver turno simultáneo
            int player2UserId = battle.Player2UserId ?? opponentUserId.Value;
            if (!battle.PendingActionsByUserId.TryGetValue(battle.PlayerUserId, out PendingBattleAction? actionP1)) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    Messages = new List<string> { "Falta la acción del jugador 1." },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (!battle.PendingActionsByUserId.TryGetValue(player2UserId, out PendingBattleAction? actionP2)) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    Messages = new List<string> { "Falta la acción del jugador 2." },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            var turnResult = ResolveSimultaneousTurn(battle, actionP1, actionP2, battle.PlayerUserId, player2UserId);

            battle.PendingActionsByUserId.Clear();
            battle.Turn++;

            return new SubmitBattleActionResult {
                Accepted = true,
                TurnResolved = true,
                Messages = turnResult.Messages,
                Timeline = turnResult.Events,
                WinnerUserId = battle.WinnerUserId
            };
        }
    }

    // Clase auxiliar para el resultado de la resolución del turno
    private class TurnResolutionResult {
        public List<string> Messages { get; set; } = new();
        public List<Networking.Messages.Battle.BattleEvent> Events { get; set; } = new();
    }

    // Métodos auxiliares para crear identificadores de Pokémon
    private Networking.Messages.Battle.PokemonIdentifier CreatePokemonIdentifier(
        BattleSession battle, 
        int userId, 
        Models.Battle.PokemonBattle pokemon) {

        bool isPlayer = battle.PlayerUserId == userId;
        return new Networking.Messages.Battle.PokemonIdentifier {
            Side = isPlayer ? "player" : "opponent",
            Slot = pokemon.Slot,
            DisplayName = pokemon.GetDisplayName()
        };
    }

    private bool IsValidActionPayload(
        BattleSession battle,
        int userId,
        BattleAction action,
        string? moveName,
        int? targetSlot,
        out string errorMessage) {

        errorMessage = string.Empty;

        BattleSide? mySide = battle.GetSideForUser(userId);
        if (mySide == null) {
            errorMessage = "No se pudo resolver tu lado de batalla.";
            return false;
        }

        var myActive = mySide.GetActivePokemon();
        if (myActive == null) {
            errorMessage = "No tienes Pokémon activo.";
            return false;
        }

        switch (action) {
            case BattleAction.Attack:
                if (string.IsNullOrWhiteSpace(moveName)) {
                    errorMessage = "Debes indicar un movimiento para atacar.";
                    return false;
                }

                var selectedMove = myActive.Movements
                    .FirstOrDefault(m => string.Equals(m.Name, moveName, StringComparison.OrdinalIgnoreCase));

                if (selectedMove == null) {
                    errorMessage = "El movimiento seleccionado no existe para tu Pokémon activo.";
                    return false;
                }

                if (!selectedMove.HasPpAvailable()) {
                    errorMessage = "Ese movimiento no tiene PP.";
                    return false;
                }

                return true;

            case BattleAction.Switch:
                if (!targetSlot.HasValue) {
                    errorMessage = "Debes indicar a qué slot quieres cambiar.";
                    return false;
                }

                int slot = targetSlot.Value;
                if (slot < 0 || slot >= mySide.Team.Count) {
                    errorMessage = "El slot de cambio no es válido.";
                    return false;
                }

                if (slot == mySide.ActiveSlot) {
                    errorMessage = "Ese Pokémon ya está activo.";
                    return false;
                }

                if (mySide.Team[slot].IsFainted()) {
                    errorMessage = "No puedes cambiar a un Pokémon debilitado.";
                    return false;
                }

                return true;

            case BattleAction.Forfeit:
                return true;

            default:
                errorMessage = "Acción de batalla no soportada.";
                return false;
        }
    }

    private TurnResolutionResult ResolveSimultaneousTurn(
        BattleSession battle,
        PendingBattleAction player1Action,
        PendingBattleAction player2Action,
        int player1UserId,
        int player2UserId) {

        var result = new TurnResolutionResult();

        // Rendirse tiene prioridad total y termina la batalla inmediatamente.
        if (player1Action.Action == BattleAction.Forfeit) {
            battle.WinnerUserId = player2UserId;
            result.Messages.Add("El jugador 1 se rindió.");
            result.Events.Add(new Networking.Messages.Battle.BattleEndEvent {
                Message = "El jugador 1 se rindió.",
                Winner = "opponent",
                WinnerUserId = player2UserId
            });
            return result;
        }
        if (player2Action.Action == BattleAction.Forfeit) {
            battle.WinnerUserId = player1UserId;
            result.Messages.Add("El jugador 2 se rindió.");
            result.Events.Add(new Networking.Messages.Battle.BattleEndEvent {
                Message = "El jugador 2 se rindió.",
                Winner = "player",
                WinnerUserId = player1UserId
            });
            return result;
        }

        List<(int userId, PendingBattleAction action)> ordered = GetActionExecutionOrder(
            battle,
            (player1UserId, player1Action),
            (player2UserId, player2Action)
        );

        foreach (var (userId, action) in ordered) {
            if (battle.IsBattleOver()) {
                break;
            }

            switch (action.Action) {
                case BattleAction.Switch:
                    ExecuteSwitch(battle, userId, action, result);
                    break;

                case BattleAction.Attack:
                    ExecuteAttack(battle, userId, action, result);
                    break;
            }

            battle.IsBattleOver();
        }

        // Aplicar efectos de estado al final del turno (solo a Pokémon activos)
        ApplyEndOfTurnEffects(battle, result);

        // Si la batalla terminó, agregar evento de fin
        if (battle.WinnerUserId != null) {
            string winner = battle.WinnerUserId == player1UserId ? "player" : "opponent";
            string winnerName = winner == "player" ? "Jugador" : "Oponente";
            result.Messages.Add($"{winnerName} ganó la batalla.");
            result.Events.Add(new Networking.Messages.Battle.BattleEndEvent {
                Message = $"{winnerName} ganó la batalla.",
                Winner = winner,
                WinnerUserId = battle.WinnerUserId
            });
        }

        return result;
    }

    private List<(int userId, PendingBattleAction action)> GetActionExecutionOrder(
        BattleSession battle,
        (int userId, PendingBattleAction action) first,
        (int userId, PendingBattleAction action) second) {

        int firstPriority = GetActionPriority(battle, first.userId, first.action);
        int secondPriority = GetActionPriority(battle, second.userId, second.action);

        if (firstPriority != secondPriority) {
            return firstPriority > secondPriority
                ? new List<(int, PendingBattleAction)> { first, second }
                : new List<(int, PendingBattleAction)> { second, first };
        }

        // Si empatan prioridad, desempatar por velocidad del Pokémon activo.
        int firstSpeed = battle.GetSideForUser(first.userId)?.GetActivePokemon()?.GetModifiedStat(Enum.StatType.Speed) ?? 0;
        int secondSpeed = battle.GetSideForUser(second.userId)?.GetActivePokemon()?.GetModifiedStat(Enum.StatType.Speed) ?? 0;

        if (firstSpeed != secondSpeed) {
            return firstSpeed > secondSpeed
                ? new List<(int, PendingBattleAction)> { first, second }
                : new List<(int, PendingBattleAction)> { second, first };
        }

        // Empate total: orden aleatorio.
        return Random.Shared.Next(0, 2) == 0
            ? new List<(int, PendingBattleAction)> { first, second }
            : new List<(int, PendingBattleAction)> { second, first };
    }

    private int GetActionPriority(BattleSession battle, int userId, PendingBattleAction action) {
        return action.Action switch {
            BattleAction.Switch => 10,
            BattleAction.Attack => battle
                .GetSideForUser(userId)
                ?.GetActivePokemon()
                ?.Movements
                .FirstOrDefault(m => string.Equals(m.Name, action.MoveName, StringComparison.OrdinalIgnoreCase))
                ?.Priority ?? 0,
            _ => 0
        };
    }

    private void ExecuteSwitch(BattleSession battle, int userId, PendingBattleAction action, TurnResolutionResult result) {
        BattleSide? mySide = battle.GetSideForUser(userId);
        if (mySide == null || !action.TargetSlot.HasValue) {
            return;
        }

        int previousSlot = mySide.ActiveSlot;
        bool switched = mySide.SwitchPokemon(action.TargetSlot.Value);
        if (!switched) {
            result.Messages.Add("No se pudo realizar el cambio de Pokémon.");
            result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                Message = "No se pudo realizar el cambio de Pokémon."
            });
            return;
        }

        var active = mySide.GetActivePokemon();
        string name = active?.GetDisplayName() ?? "Pokémon";
        string side = battle.PlayerUserId == userId ? "player" : "opponent";
        string message = $"Cambio realizado: entra {name}.";

        result.Messages.Add(message);
        result.Events.Add(new Networking.Messages.Battle.SwitchEvent {
            Message = message,
            Side = side,
            PreviousActiveSlot = previousSlot,
            NewActiveSlot = action.TargetSlot.Value,
            NewPokemonName = name,
            IsAutomatic = false
        });
    }

    private void ExecuteAttack(BattleSession battle, int userId, PendingBattleAction action, TurnResolutionResult result) {
        BattleSide? mySide = battle.GetSideForUser(userId);
        BattleSide? rivalSide = battle.GetOpponentSideForUser(userId);
        if (mySide == null || rivalSide == null) {
            return;
        }

        PokemonBattle? attacker = mySide.GetActivePokemon();
        PokemonBattle? defender = rivalSide.GetActivePokemon();
        if (attacker == null || defender == null) {
            return;
        }

        if (attacker.IsFainted()) {
            string msg = $"{attacker.GetDisplayName()} está debilitado y no puede atacar.";
            result.Messages.Add(msg);
            result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                Message = msg
            });
            return;
        }

        var attackerId = CreatePokemonIdentifier(battle, userId, attacker);
        int? rivalUserId = battle.GetOpponentUserId(userId);
        var defenderId = rivalUserId.HasValue ? CreatePokemonIdentifier(battle, rivalUserId.Value, defender) : null;

        // Verificar si el Pokémon puede atacar (estados como Freeze, Sleep, Paralysis, Confusion)
        var (canAttack, statusMessage) = attacker.CanAttack();
        if (statusMessage != null) {
            result.Messages.Add(statusMessage);

            // Si no puede atacar, registrar evento de bloqueo
            if (!canAttack) {
                var move = attacker.Movements
                    .FirstOrDefault(m => string.Equals(m.Name, action.MoveName, StringComparison.OrdinalIgnoreCase));

                string blockReason = attacker.Status switch {
                    Enum.PokeStatus.Freeze => "frozen",
                    Enum.PokeStatus.Sleep => "asleep",
                    Enum.PokeStatus.Paralysis => "paralyzed",
                    _ => attacker.HasSecondaryStatus(Enum.PokeSecondaryStatus.Confuse) ? "confused" : "unknown"
                };

                if (defenderId != null) {
                    result.Events.Add(new Networking.Messages.Battle.AttackEvent {
                        Message = statusMessage,
                        Attacker = attackerId,
                        Defender = defenderId,
                        MoveName = move?.Name ?? action.MoveName ?? "Unknown",
                        Hit = false,
                        Blocked = true,
                        BlockReason = blockReason
                    });
                }

                // Si se golpeó a sí mismo por confusión, ya se generó el evento de HP en CanAttack
                return;
            }
        }

        var selectedMove = attacker.Movements
            .FirstOrDefault(m => string.Equals(m.Name, action.MoveName, StringComparison.OrdinalIgnoreCase));

        if (selectedMove == null || !selectedMove.HasPpAvailable()) {
            string msg = $"{attacker.GetDisplayName()} no pudo usar el movimiento.";
            result.Messages.Add(msg);
            result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                Message = msg
            });
            return;
        }

        int hpBefore = defender.CurrentHp;
        selectedMove.ExecuteMovement(attacker, defender);
        int hpAfter = defender.CurrentHp;
        int damage = Math.Max(0, hpBefore - hpAfter);

        string attackMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}. Daño: {damage}.";
        result.Messages.Add(attackMsg);

        if (defenderId != null) {
            result.Events.Add(new Networking.Messages.Battle.AttackEvent {
                Message = attackMsg,
                Attacker = attackerId,
                Defender = defenderId,
                MoveName = selectedMove.Name,
                Hit = damage > 0 || hpBefore == hpAfter, // Hit si causó daño o el HP no cambió (ej: inmunidad)
                Blocked = false
            });

            if (damage > 0) {
                result.Events.Add(new Networking.Messages.Battle.HpChangeEvent {
                    Message = $"{defender.GetDisplayName()} pierde {damage} PS.",
                    Target = defenderId,
                    BeforeHp = hpBefore,
                    AfterHp = hpAfter,
                    MaxHp = defender.MaxHp,
                    Amount = -damage,
                    Cause = "move",
                    SourceMove = selectedMove.Name,
                    SourcePokemon = attackerId
                });
            }
        }

        if (defender.IsFainted()) {
            string faintMsg = $"{defender.GetDisplayName()} se debilitó.";
            result.Messages.Add(faintMsg);

            if (defenderId != null) {
                result.Events.Add(new Networking.Messages.Battle.FaintEvent {
                    Message = faintMsg,
                    Target = defenderId
                });
            }

            int? autoSwitchSlot = rivalSide.GetFirstNonFaintedSlot();
            if (autoSwitchSlot.HasValue) {
                int prevSlot = rivalSide.ActiveSlot;
                rivalSide.SwitchPokemon(autoSwitchSlot.Value);
                var newActive = rivalSide.GetActivePokemon();
                string switchMsg = $"Entra {newActive?.GetDisplayName() ?? "Pokémon"} automáticamente.";
                result.Messages.Add(switchMsg);

                string rivalSideStr = battle.PlayerUserId == userId ? "opponent" : "player";
                result.Events.Add(new Networking.Messages.Battle.SwitchEvent {
                    Message = switchMsg,
                    Side = rivalSideStr,
                    PreviousActiveSlot = prevSlot,
                    NewActiveSlot = autoSwitchSlot.Value,
                    NewPokemonName = newActive?.GetDisplayName() ?? "Pokémon",
                    IsAutomatic = true
                });
            }
        }
    }

    // Aplica efectos de estado al final del turno para ambos Pokémon activos
    private void ApplyEndOfTurnEffects(BattleSession battle, TurnResolutionResult result) {
        // Aplicar efectos al Pokémon activo del jugador 1
        ApplyEndOfTurnEffectsForPokemon(battle, battle.PlayerUserId, battle.PlayerSide, result);

        // Verificar si la batalla terminó antes de aplicar efectos al jugador 2
        if (battle.IsBattleOver()) {
            return;
        }

        // Aplicar efectos al Pokémon activo del jugador 2
        int? player2Id = battle.Player2UserId ?? battle.GetOpponentUserId(battle.PlayerUserId);
        if (player2Id.HasValue) {
            ApplyEndOfTurnEffectsForPokemon(battle, player2Id.Value, battle.OpponentSide, result);
        }

        // Verificar si la batalla terminó después de los efectos
        battle.IsBattleOver();
    }

    private void ApplyEndOfTurnEffectsForPokemon(
        BattleSession battle,
        int userId,
        BattleSide side,
        TurnResolutionResult result) {

        var pokemon = side.GetActivePokemon();
        if (pokemon == null || pokemon.IsFainted()) {
            return;
        }

        var pokemonId = CreatePokemonIdentifier(battle, userId, pokemon);

        // Aplicar efectos primarios (burn, poison, etc.)
        int hpBefore = pokemon.CurrentHp;
        string? effect = pokemon.ApplyEndOfTurnStatusEffect();
        int hpAfter = pokemon.CurrentHp;

        if (effect != null) {
            result.Messages.Add(effect);

            if (hpBefore != hpAfter) {
                string cause = pokemon.Status switch {
                    Enum.PokeStatus.Burn => "burn",
                    Enum.PokeStatus.Poison => "poison",
                    Enum.PokeStatus.BadlyPoisoned => "badly_poisoned",
                    _ => "status_effect"
                };

                int damage = Math.Max(0, hpBefore - hpAfter);
                result.Events.Add(new Networking.Messages.Battle.HpChangeEvent {
                    Message = effect,
                    Target = pokemonId,
                    BeforeHp = hpBefore,
                    AfterHp = hpAfter,
                    MaxHp = pokemon.MaxHp,
                    Amount = -damage,
                    Cause = cause
                });
            }

            // Verificar si se debilitó por el efecto de estado
            if (pokemon.IsFainted()) {
                string faintMsg = $"{pokemon.GetDisplayName()} se debilitó.";
                result.Messages.Add(faintMsg);
                result.Events.Add(new Networking.Messages.Battle.FaintEvent {
                    Message = faintMsg,
                    Target = pokemonId
                });

                int? autoSwitchSlot = side.GetFirstNonFaintedSlot();
                if (autoSwitchSlot.HasValue) {
                    int prevSlot = side.ActiveSlot;
                    side.SwitchPokemon(autoSwitchSlot.Value);
                    var newActive = side.GetActivePokemon();
                    string switchMsg = $"Entra {newActive?.GetDisplayName() ?? "Pokémon"} automáticamente.";
                    result.Messages.Add(switchMsg);

                    string sideStr = battle.PlayerUserId == userId ? "player" : "opponent";
                    result.Events.Add(new Networking.Messages.Battle.SwitchEvent {
                        Message = switchMsg,
                        Side = sideStr,
                        PreviousActiveSlot = prevSlot,
                        NewActiveSlot = autoSwitchSlot.Value,
                        NewPokemonName = newActive?.GetDisplayName() ?? "Pokémon",
                        IsAutomatic = true
                    });
                }
                return; // No aplicar efectos secundarios si está debilitado
            }
        }

        // Aplicar efectos secundarios (leech seed, etc.)
        hpBefore = pokemon.CurrentHp;
        string? secondaryEffect = pokemon.ApplyEndOfTurnSecondaryStatusEffect();
        hpAfter = pokemon.CurrentHp;

        if (secondaryEffect != null) {
            result.Messages.Add(secondaryEffect);

            if (hpBefore != hpAfter && pokemon.HasSecondaryStatus(Enum.PokeSecondaryStatus.Seeded)) {
                int damage = Math.Max(0, hpBefore - hpAfter);

                // Crear evento de HP para el Pokémon afectado
                result.Events.Add(new Networking.Messages.Battle.HpChangeEvent {
                    Message = $"{pokemon.GetDisplayName()} pierde {damage} PS por Drenadoras.",
                    Target = pokemonId,
                    BeforeHp = hpBefore,
                    AfterHp = hpAfter,
                    MaxHp = pokemon.MaxHp,
                    Amount = -damage,
                    Cause = "leech_seed",
                    SourcePokemon = pokemon.LeechSeedSource != null 
                        ? CreatePokemonIdentifier(battle, 
                            battle.PlayerUserId == userId ? (battle.Player2UserId ?? 0) : battle.PlayerUserId,
                            pokemon.LeechSeedSource)
                        : null
                });

                // Si la fuente del Leech Seed también se curó, agregar evento de curación
                if (pokemon.LeechSeedSource != null && !pokemon.LeechSeedSource.IsFainted()) {
                    var sourceId = CreatePokemonIdentifier(battle,
                        battle.PlayerUserId == userId ? (battle.Player2UserId ?? 0) : battle.PlayerUserId,
                        pokemon.LeechSeedSource);

                    result.Events.Add(new Networking.Messages.Battle.HpChangeEvent {
                        Message = $"{pokemon.LeechSeedSource.GetDisplayName()} recupera {damage} PS.",
                        Target = sourceId,
                        BeforeHp = pokemon.LeechSeedSource.CurrentHp - damage,
                        AfterHp = pokemon.LeechSeedSource.CurrentHp,
                        MaxHp = pokemon.LeechSeedSource.MaxHp,
                        Amount = damage,
                        Cause = "leech_seed",
                        SourcePokemon = pokemonId
                    });
                }
            }

            // Verificar si se debilitó por el efecto secundario
            if (pokemon.IsFainted()) {
                string faintMsg = $"{pokemon.GetDisplayName()} se debilitó.";
                result.Messages.Add(faintMsg);
                result.Events.Add(new Networking.Messages.Battle.FaintEvent {
                    Message = faintMsg,
                    Target = pokemonId
                });

                int? autoSwitchSlot = side.GetFirstNonFaintedSlot();
                if (autoSwitchSlot.HasValue) {
                    int prevSlot = side.ActiveSlot;
                    side.SwitchPokemon(autoSwitchSlot.Value);
                    var newActive = side.GetActivePokemon();
                    string switchMsg = $"Entra {newActive?.GetDisplayName() ?? "Pokémon"} automáticamente.";
                    result.Messages.Add(switchMsg);

                    string sideStr = battle.PlayerUserId == userId ? "player" : "opponent";
                    result.Events.Add(new Networking.Messages.Battle.SwitchEvent {
                        Message = switchMsg,
                        Side = sideStr,
                        PreviousActiveSlot = prevSlot,
                        NewActiveSlot = autoSwitchSlot.Value,
                        NewPokemonName = newActive?.GetDisplayName() ?? "Pokémon",
                        IsAutomatic = true
                    });
                }
            }
        }
    }
}