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
        public List<string> Messages { get; set; } = new(); // Legacy (deprecated)
        public List<StructuredBattleMessage> StructuredMessages { get; set; } = new(); // Nuevo
        public List<Networking.Messages.Battle.BattleEvent> Timeline { get; set; } = new();
        public int? WinnerUserId { get; set; }
    }

    // Clase interna para resolver turnos
    private class TurnResolutionResult {
        public List<string> Messages { get; set; } = new();
        public List<StructuredBattleMessage> StructuredMessages { get; set; } = new();
        public List<Networking.Messages.Battle.BattleEvent> Events { get; set; } = new();
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
                StructuredMessages = turnResult.StructuredMessages,
                Timeline = turnResult.Events,
                WinnerUserId = battle.WinnerUserId
            };
        }
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

    // Helper para crear nombres normalizados de Pokemon (sin tildes, minusculas)
    private string GetNormalizedPokemonName(Models.Battle.PokemonBattle pokemon) {
        string displayName = pokemon.GetDisplayName();
        return Utils.TextNormalizer.NormalizePokemonName(displayName);
    }

    // Helper para obtener el nombre del owner (player/opponent)
    private string GetOwnerName(BattleSession battle, int userId) {
        return battle.PlayerUserId == userId ? "player" : "opponent";
    }

    // Helper para crear args basicos de actor
    private Dictionary<string, object> CreateActorArgs(
        BattleSession battle, 
        int userId, 
        Models.Battle.PokemonBattle pokemon) {

        return new Dictionary<string, object> {
            { "actor", GetNormalizedPokemonName(pokemon) },
            { "owner", GetOwnerName(battle, userId) }
        };
    }

    // Helper para crear args de actor + target
    private Dictionary<string, object> CreateActorTargetArgs(
        BattleSession battle,
        int attackerUserId,
        Models.Battle.PokemonBattle attacker,
        int defenderUserId,
        Models.Battle.PokemonBattle defender) {

        return new Dictionary<string, object> {
            { "actor", GetNormalizedPokemonName(attacker) },
            { "actor_owner", GetOwnerName(battle, attackerUserId) },
            { "target", GetNormalizedPokemonName(defender) },
            { "target_owner", GetOwnerName(battle, defenderUserId) }
        };
    }

    // Helper para añadir mensaje estructurado
    private void AddStructuredMessage(
        SubmitBattleActionResult result,
        string code,
        Dictionary<string, object>? args = null) {

        result.StructuredMessages.Add(BattleMessageBuilder.Create(code, args));
    }

    // Sobrecarga para TurnResolutionResult
    private void AddStructuredMessage(
        TurnResolutionResult result,
        string code,
        Dictionary<string, object>? args = null) {

        result.StructuredMessages.Add(BattleMessageBuilder.Create(code, args));
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

                // Generar mensaje estructurado segun el estado
                var actorArgs = CreateActorArgs(battle, userId, attacker);

                if (attacker.Status == Enum.PokeStatus.Freeze) {
                    AddStructuredMessage(result, BattleMessageCode.FrozenSolid, actorArgs);
                } else if (attacker.Status == Enum.PokeStatus.Sleep) {
                    AddStructuredMessage(result, BattleMessageCode.FastAsleep, actorArgs);
                } else if (attacker.Status == Enum.PokeStatus.Paralysis) {
                    AddStructuredMessage(result, BattleMessageCode.ParalyzedCantMove, actorArgs);
                } else if (attacker.HasSecondaryStatus(Enum.PokeSecondaryStatus.Confuse)) {
                    // Confusion self-hit: NO emitir attack_used
                    // El daño ya fue aplicado en CanAttack(), solo registramos el evento
                    // Extraer el daño del mensaje de estado si es posible
                    int confusionDamage = 0;
                    if (statusMessage.Contains(" por ")) {
                        var parts = statusMessage.Split(" por ");
                        if (parts.Length > 1) {
                            var damagePart = parts[1].Replace(" PS.", "").Trim();
                            int.TryParse(damagePart, out confusionDamage);
                        }
                    }

                    var confusionArgs = new Dictionary<string, object>(actorArgs) {
                        { "damage", confusionDamage }
                    };
                    AddStructuredMessage(result, BattleMessageCode.ConfusionSelfHit, confusionArgs);
                }

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

            // Puede atacar pero hay un mensaje (ej: "se despertó", "se descongeló", "curó confusión")
            var actorArgs2 = CreateActorArgs(battle, userId, attacker);

            if (statusMessage.Contains("despertado")) {
                AddStructuredMessage(result, BattleMessageCode.WokeUp, actorArgs2);
            } else if (statusMessage.Contains("descongelado")) {
                AddStructuredMessage(result, BattleMessageCode.Thawed, actorArgs2);
            } else if (statusMessage.Contains("confundido") && statusMessage.Contains("ya no")) {
                AddStructuredMessage(result, BattleMessageCode.ConfusionEnd, actorArgs2);
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

        int hpBeforeAttacker = attacker.CurrentHp;
        int hpBefore = defender.CurrentHp;

        // Capturar stages antes del movimiento (para detectar cambios de stats)
        var attackerStagesBefore = new Dictionary<string, int> {
            { "Attack", attacker.AttackStage },
            { "Defense", attacker.DefenseStage },
            { "SpecialAttack", attacker.SpecialAttackStage },
            { "SpecialDefense", attacker.SpecialDefenseStage },
            { "Speed", attacker.SpeedStage },
            { "Accuracy", attacker.AccuracyStage },
            { "Evasion", attacker.EvasionStage }
        };

        var defenderStagesBefore = new Dictionary<string, int> {
            { "Attack", defender.AttackStage },
            { "Defense", defender.DefenseStage },
            { "SpecialAttack", defender.SpecialAttackStage },
            { "SpecialDefense", defender.SpecialDefenseStage },
            { "Speed", defender.SpeedStage },
            { "Accuracy", defender.AccuracyStage },
            { "Evasion", defender.EvasionStage }
        };

        selectedMove.ExecuteMovement(attacker, defender);

        int hpAfter = defender.CurrentHp;
        int hpAfterAttacker = attacker.CurrentHp;
        int damage = Math.Max(0, hpBefore - hpAfter);

        // Aplicar reducción de daño por efectos de campo en el lado del defensor
        // Light Screen (ID 113): reduce daño de ataques especiales
        // Reflect (ID 115): reduce daño de ataques físicos
        if (damage > 0 && rivalSide != null) {
            bool damageReduced = false;

            // Light Screen: protege contra ataques especiales
            if (rivalSide.LightScreenTurnsRemaining > 0 && 
                selectedMove.MovementClass == Enum.MovementClass.Special) {
                damage = damage / 2;
                damageReduced = true;
            }

            // Reflect: protege contra ataques físicos
            if (rivalSide.ReflectTurnsRemaining > 0 && 
                selectedMove.MovementClass == Enum.MovementClass.Physical) {
                damage = damage / 2;
                damageReduced = true;
            }

            // Si el daño fue reducido, recalcular el HP del defensor
            if (damageReduced) {
                defender.CurrentHp = hpBefore - damage;
                if (defender.CurrentHp < 0) {
                    defender.CurrentHp = 0;
                }
                hpAfter = defender.CurrentHp;
            }
        }

        int healing = Math.Max(0, hpAfterAttacker - hpBeforeAttacker);

        // Detectar cambios de estadísticas del atacante
        var attackerStatChanges = new List<(string stat, int change, int newStage)>();
        var attackerStagesAfter = new Dictionary<string, int> {
            { "Attack", attacker.AttackStage },
            { "Defense", attacker.DefenseStage },
            { "SpecialAttack", attacker.SpecialAttackStage },
            { "SpecialDefense", attacker.SpecialDefenseStage },
            { "Speed", attacker.SpeedStage },
            { "Accuracy", attacker.AccuracyStage },
            { "Evasion", attacker.EvasionStage }
        };

        foreach (var (stat, stageBefore) in attackerStagesBefore) {
            int stageAfter = attackerStagesAfter[stat];
            if (stageBefore != stageAfter) {
                attackerStatChanges.Add((stat, stageAfter - stageBefore, stageAfter));
            }
        }

        // Detectar cambios de estadísticas del defensor
        var defenderStatChanges = new List<(string stat, int change, int newStage)>();
        var defenderStagesAfter = new Dictionary<string, int> {
            { "Attack", defender.AttackStage },
            { "Defense", defender.DefenseStage },
            { "SpecialAttack", defender.SpecialAttackStage },
            { "SpecialDefense", defender.SpecialDefenseStage },
            { "Speed", defender.SpeedStage },
            { "Accuracy", defender.AccuracyStage },
            { "Evasion", defender.EvasionStage }
        };

        foreach (var (stat, stageBefore) in defenderStagesBefore) {
            int stageAfter = defenderStagesAfter[stat];
            if (stageBefore != stageAfter) {
                // Verificar si Mist está activo y el cambio es negativo (reducción de estadística)
                if (rivalSide != null && rivalSide.MistTurnsRemaining > 0 && stageAfter < stageBefore) {
                    // Mist bloquea las reducciones de estadísticas del enemigo
                    // Revertir el cambio
                    switch (stat) {
                        case "Attack":
                            defender.AttackStage = stageBefore;
                            break;
                        case "Defense":
                            defender.DefenseStage = stageBefore;
                            break;
                        case "SpecialAttack":
                            defender.SpecialAttackStage = stageBefore;
                            break;
                        case "SpecialDefense":
                            defender.SpecialDefenseStage = stageBefore;
                            break;
                        case "Speed":
                            defender.SpeedStage = stageBefore;
                            break;
                        case "Accuracy":
                            defender.AccuracyStage = stageBefore;
                            break;
                        case "Evasion":
                            defender.EvasionStage = stageBefore;
                            break;
                    }

                    // Emitir mensaje de bloqueo por Mist
                    string mistBlockMsg = $"¡La niebla protege las estadísticas de {defender.GetDisplayName()}!";
                    result.Messages.Add(mistBlockMsg);

                    // Mensaje estructurado
                    if (rivalUserId.HasValue) {
                        var mistArgs = CreateActorArgs(battle, rivalUserId.Value, defender);
                        AddStructuredMessage(result, BattleMessageCode.StatProtected, mistArgs);
                    }

                    result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                        Message = mistBlockMsg
                    });

                    // Actualizar la lista para no registrar el cambio
                    // (el cambio fue bloqueado)
                    continue;
                }

                defenderStatChanges.Add((stat, stageAfter - stageBefore, stageAfter));
            }
        }

        // Mensaje para movimientos de daño con curación (damage+heal)
        if (damage > 0 && healing > 0) {
            string attackMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}. Daño: {damage}. Recupera {healing} PS.";
            result.Messages.Add(attackMsg);

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            AddStructuredMessage(result, BattleMessageCode.AttackUsed, attackArgs);

            if (rivalUserId.HasValue) {
                var damageArgs = new Dictionary<string, object>(CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender)) {
                    { "damage", damage }
                };
                AddStructuredMessage(result, BattleMessageCode.DamageDealt, damageArgs);

                var drainArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                    { "amount", healing }
                };
                AddStructuredMessage(result, BattleMessageCode.DrainHp, drainArgs);
            }

            if (defenderId != null) {
                result.Events.Add(new Networking.Messages.Battle.AttackEvent {
                    Message = attackMsg,
                    Attacker = attackerId,
                    Defender = defenderId,
                    MoveName = selectedMove.Name,
                    Hit = true,
                    Blocked = false
                });

                // Evento de daño al defensor
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

                // Evento de curación al atacante
                result.Events.Add(new Networking.Messages.Battle.HpChangeEvent {
                    Message = $"{attacker.GetDisplayName()} recupera {healing} PS.",
                    Target = attackerId,
                    BeforeHp = hpBeforeAttacker,
                    AfterHp = hpAfterAttacker,
                    MaxHp = attacker.MaxHp,
                    Amount = healing,
                    Cause = "drain",
                    SourceMove = selectedMove.Name,
                    SourcePokemon = attackerId
                });
            }
        }
        // Mensaje para movimientos de solo daño
        else if (damage > 0) {
            string attackMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}. Daño: {damage}.";
            result.Messages.Add(attackMsg);

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            AddStructuredMessage(result, BattleMessageCode.AttackUsed, attackArgs);

            if (rivalUserId.HasValue) {
                var damageArgs = new Dictionary<string, object>(CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender)) {
                    { "damage", damage }
                };
                AddStructuredMessage(result, BattleMessageCode.DamageDealt, damageArgs);
            }

            if (defenderId != null) {
                result.Events.Add(new Networking.Messages.Battle.AttackEvent {
                    Message = attackMsg,
                    Attacker = attackerId,
                    Defender = defenderId,
                    MoveName = selectedMove.Name,
                    Hit = true,
                    Blocked = false
                });

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

                // Emitir eventos de cambio de estadísticas del defensor si ocurrieron
                foreach (var (stat, change, newStage) in defenderStatChanges) {
                    string statMsg = change > 0 
                        ? $"{defender.GetDisplayName()} aumenta {stat} en {change}."
                        : $"{defender.GetDisplayName()} reduce {stat} en {Math.Abs(change)}.";
                    result.Messages.Add(statMsg);

                    // Mensaje estructurado para cambios de stats
                    var statArgs = new Dictionary<string, object>(CreateActorArgs(battle, rivalUserId.Value, defender)) {
                        { "stat", Utils.TextNormalizer.ToSnakeCase(stat) },
                        { "stages", Math.Abs(change) }
                    };
                    string statCode = change > 0 ? BattleMessageCode.StatRose : BattleMessageCode.StatFell;
                    AddStructuredMessage(result, statCode, statArgs);

                    result.Events.Add(new Networking.Messages.Battle.StatStageChangeEvent {
                        Message = statMsg,
                        Target = defenderId,
                        Stat = stat,
                        Change = change,
                        NewStage = newStage
                    });
                }
            }
        }
        // Mensaje para movimientos de solo curación
        else if (healing > 0) {
            string healMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}. Recupera {healing} PS.";
            result.Messages.Add(healMsg);

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            AddStructuredMessage(result, BattleMessageCode.AttackUsed, attackArgs);

            var healArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "amount", healing }
            };
            AddStructuredMessage(result, BattleMessageCode.HpRestored, healArgs);

            result.Events.Add(new Networking.Messages.Battle.AttackEvent {
                Message = healMsg,
                Attacker = attackerId,
                Defender = attackerId, // El defensor es el mismo atacante
                MoveName = selectedMove.Name,
                Hit = true,
                Blocked = false
            });

            result.Events.Add(new Networking.Messages.Battle.HpChangeEvent {
                Message = $"{attacker.GetDisplayName()} recupera {healing} PS.",
                Target = attackerId,
                BeforeHp = hpBeforeAttacker,
                AfterHp = hpAfterAttacker,
                MaxHp = attacker.MaxHp,
                Amount = healing,
                Cause = "move",
                SourceMove = selectedMove.Name,
                SourcePokemon = attackerId
            });
        }
        // Movimientos sin efecto visible de HP (stat changes, whole-field-effect, etc.)
        else if (defenderId != null) {
            string moveMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}.";
            result.Messages.Add(moveMsg);

            // Mensaje estructurado de uso de ataque
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            AddStructuredMessage(result, BattleMessageCode.AttackUsed, attackArgs);

            result.Events.Add(new Networking.Messages.Battle.AttackEvent {
                Message = moveMsg,
                Attacker = attackerId,
                Defender = defenderId,
                MoveName = selectedMove.Name,
                Hit = true,
                Blocked = false
            });

            // Caso especial: Haze (ID 114) resetea todas las características
            // Emitir un mensaje general en lugar de listar cada cambio
            if (selectedMove.Id == 114 && (defenderStatChanges.Count > 0 || attackerStatChanges.Count > 0)) {
                string hazeMsg = "Las características de los Pokémon han vuelto a sus valores originales.";
                result.Messages.Add(hazeMsg);

                // Mensaje estructurado para reset de stats
                AddStructuredMessage(result, BattleMessageCode.StatsReset, new Dictionary<string, object>());
                result.Messages.Add(hazeMsg);

                result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                    Message = hazeMsg
                });
            }
            // Para otros movimientos, emitir eventos individuales de cambio de estadísticas
            else {
                // Emitir eventos de cambio de estadísticas del defensor si ocurrieron
                foreach (var (stat, change, newStage) in defenderStatChanges) {
                    string statMsg = change > 0 
                        ? $"{defender.GetDisplayName()} aumenta {stat} en {change}."
                        : $"{defender.GetDisplayName()} reduce {stat} en {Math.Abs(change)}.";
                    result.Messages.Add(statMsg);

                    result.Events.Add(new Networking.Messages.Battle.StatStageChangeEvent {
                        Message = statMsg,
                        Target = defenderId,
                        Stat = stat,
                        Change = change,
                        NewStage = newStage
                    });
                }
            }
        }

        // Emitir eventos de cambio de estadísticas del atacante si ocurrieron
        // Esto es importante para movimientos como stat-boosters del usuario
        // Para Haze, estos eventos no se emiten porque ya se emitió el mensaje general
        if (selectedMove.Id != 114) {
            foreach (var (stat, change, newStage) in attackerStatChanges) {
                string statMsg = change > 0 
                    ? $"{attacker.GetDisplayName()} aumenta {stat} en {change}."
                    : $"{attacker.GetDisplayName()} reduce {stat} en {Math.Abs(change)}.";
                result.Messages.Add(statMsg);

                result.Events.Add(new Networking.Messages.Battle.StatStageChangeEvent {
                    Message = statMsg,
                    Target = attackerId,
                    Stat = stat,
                    Change = change,
                    NewStage = newStage
                });
            }
        }

        // Manejar cambio forzado (force-switch) si el movimiento es de esa categoría
        // Solo si el defensor no está debilitado
        if (selectedMove.Category == "force-switch" && !defender.IsFainted() && defenderId != null) {
            // Buscar otros Pokémon disponibles en el equipo rival
            var availableSlots = new List<int>();
            for (int i = 0; i < rivalSide.Team.Count; i++) {
                if (i != rivalSide.ActiveSlot && !rivalSide.Team[i].IsFainted()) {
                    availableSlots.Add(i);
                }
            }

            // Si hay Pokémon disponibles, forzar cambio aleatorio
            if (availableSlots.Count > 0) {
                Random random = new Random();
                int randomSlot = availableSlots[random.Next(availableSlots.Count)];

                int prevSlot = rivalSide.ActiveSlot;
                rivalSide.SwitchPokemon(randomSlot);
                var newActive = rivalSide.GetActivePokemon();

                string forcedSwitchMsg = $"{defender.GetDisplayName()} es forzado a retirarse. Entra {newActive?.GetDisplayName() ?? "Pokémon"}.";
                result.Messages.Add(forcedSwitchMsg);

                string rivalSideStr = battle.PlayerUserId == userId ? "opponent" : "player";
                result.Events.Add(new Networking.Messages.Battle.SwitchEvent {
                    Message = forcedSwitchMsg,
                    Side = rivalSideStr,
                    PreviousActiveSlot = prevSlot,
                    NewActiveSlot = randomSlot,
                    NewPokemonName = newActive?.GetDisplayName() ?? "Pokémon",
                    IsAutomatic = true
                });
            }
            // Si no hay Pokémon disponibles, el movimiento no tiene efecto adicional
            // (el movimiento ya se ejecutó y consumió PP, pero no hubo cambio)
        }

        // Manejar efectos de campo (field-effect) si el movimiento es de esa categoría
        if (selectedMove.Category == "field-effect") {
            // Mist (ID 54): Protege las estadísticas del usuario de ser reducidas por el enemigo durante 5 turnos
            if (selectedMove.Id == 54) {
                mySide.MistTurnsRemaining = 5;
                string mistMsg = $"¡{attacker.GetDisplayName()} se ha rodeado de niebla!";
                result.Messages.Add(mistMsg);

                // Mensaje estructurado
                var mistArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                    { "turns", 5 }
                };
                AddStructuredMessage(result, BattleMessageCode.MistStart, mistArgs);

                result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                    Message = mistMsg
                });
            }
            // Light Screen (ID 113): Reduce el daño de ataques especiales enemigos a la mitad durante 5 turnos
            else if (selectedMove.Id == 113) {
                mySide.LightScreenTurnsRemaining = 5;
                string lightScreenMsg = $"¡Pantalla de Luz reduce el daño de ataques especiales!";
                result.Messages.Add(lightScreenMsg);

                // Mensaje estructurado
                var lightScreenArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) },
                    { "turns", 5 }
                };
                AddStructuredMessage(result, BattleMessageCode.LightScreenStart, lightScreenArgs);

                result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                    Message = lightScreenMsg
                });
            }
            // Reflect (ID 115): Reduce el daño de ataques físicos enemigos a la mitad durante 5 turnos
            else if (selectedMove.Id == 115) {
                mySide.ReflectTurnsRemaining = 5;
                string reflectMsg = $"¡Reflejo reduce el daño de ataques físicos!";
                result.Messages.Add(reflectMsg);

                // Mensaje estructurado
                var reflectArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) },
                    { "turns", 5 }
                };
                AddStructuredMessage(result, BattleMessageCode.ReflectStart, reflectArgs);

                result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                    Message = reflectMsg
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
        // Decrementar contadores de efectos de campo para ambos lados
        DecrementFieldEffects(battle, battle.PlayerSide, battle.PlayerUserId, result);

        int? opponentUserId = battle.Player2UserId ?? battle.GetOpponentUserId(battle.PlayerUserId);
        if (opponentUserId.HasValue) {
            DecrementFieldEffects(battle, battle.OpponentSide, opponentUserId.Value, result);
        }

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

    // Decrementa los contadores de efectos de campo y emite mensajes cuando expiran
    private void DecrementFieldEffects(BattleSession battle, BattleSide side, int userId, TurnResolutionResult result) {
        // Mist
        if (side.MistTurnsRemaining > 0) {
            side.MistTurnsRemaining--;
            if (side.MistTurnsRemaining == 0) {
                string mistEndMsg = "¡El efecto de niebla se disipó!";
                result.Messages.Add(mistEndMsg);

                // Mensaje estructurado
                var mistArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) }
                };
                AddStructuredMessage(result, BattleMessageCode.MistEnd, mistArgs);

                result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                    Message = mistEndMsg
                });
            }
        }

        // Light Screen
        if (side.LightScreenTurnsRemaining > 0) {
            side.LightScreenTurnsRemaining--;
            if (side.LightScreenTurnsRemaining == 0) {
                string lightScreenEndMsg = "¡El efecto de Pantalla de Luz se disipó!";
                result.Messages.Add(lightScreenEndMsg);

                // Mensaje estructurado
                var lightScreenArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) }
                };
                AddStructuredMessage(result, BattleMessageCode.LightScreenEnd, lightScreenArgs);

                result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                    Message = lightScreenEndMsg
                });
            }
        }

        // Reflect
        if (side.ReflectTurnsRemaining > 0) {
            side.ReflectTurnsRemaining--;
            if (side.ReflectTurnsRemaining == 0) {
                string reflectEndMsg = "¡El efecto de Reflejo se disipó!";
                result.Messages.Add(reflectEndMsg);

                // Mensaje estructurado
                var reflectArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) }
                };
                AddStructuredMessage(result, BattleMessageCode.ReflectEnd, reflectArgs);

                result.Events.Add(new Networking.Messages.Battle.MessageEvent {
                    Message = reflectEndMsg
                });
            }
        }
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