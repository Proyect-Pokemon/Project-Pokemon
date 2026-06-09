using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Battle;
using ProjectPokemon.Models.Battle.Movements;
using ProjectPokemon.Networking.Messages.Battle;
using ProjectPokemon.Services.Battle;

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

        // Lista ordenada de pasos de replay para reproducción determinista
        public List<ReplayStep> ReplaySteps { get; set; } = new();

        public int? WinnerUserId { get; set; }

        // Indica si el jugador necesita elegir un Pokémon de reemplazo
        public bool RequiresSwitchSelection { get; set; }

        // Slots disponibles para cambio forzoso (solo Pokémon no debilitados)
        public List<int> AvailableSlotsForSwitch { get; set; } = new();
    }

    // Clase interna para resolver turnos
    private class TurnResolutionResult {
        /// <summary>
        /// Builder para construir steps ordenados durante la resolución del turno
        /// </summary>
        public ReplayStepBuilder StepBuilder { get; set; } = new();
    }

    // Crea una nueva batalla cargando el equipo del usuario (vs CPU con placeholder)
    public async Task<BattleSession?> StartBattleAsync(int userId, int teamId) {
        var playerTeam = await LoadTeamAsync(userId, teamId);
        if (playerTeam == null) return null;

        // Cargar nombre del jugador
        var player = await _context.Users.FindAsync(userId);
        string playerName = player?.Nickname ?? "Jugador";

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
            PlayerUserName = playerName,
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

        // Cargar nombres de ambos jugadores
        var player1 = await _context.Users.FindAsync(player1UserId);
        var player2 = await _context.Users.FindAsync(player2UserId);
        string player1Name = player1?.Nickname ?? "Jugador 1";
        string player2Name = player2?.Nickname ?? "Jugador 2";

        var session = new BattleSession {
            PlayerUserId = player1UserId,
            Player2UserId = player2UserId,
            PlayerUserName = player1Name,
            Player2UserName = player2Name,
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
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = "No perteneces a esta batalla." } },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (battle.WinnerUserId != null) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = "La batalla ya ha terminado." } },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (action == BattleAction.Forfeit) {
                int? forfeitOpponentUserId = battle.GetOpponentUserId(userId);
                if (!forfeitOpponentUserId.HasValue) {
                    return new SubmitBattleActionResult {
                        Accepted = false,
                        TurnResolved = false,
                        ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = "No hay rival asignado." } },
                        WinnerUserId = battle.WinnerUserId
                    };
                }

                battle.WinnerUserId = forfeitOpponentUserId.Value;
                battle.Status = ProjectPokemon.Enum.BattleStatus.Finished;
                battle.PendingActionsByUserId.Clear();

                // Obtener nombre del jugador que se rindió
                string forfeitPlayerName = userId == battle.PlayerUserId 
                    ? (battle.PlayerUserName ?? "Jugador") 
                    : (battle.Player2UserName ?? "Jugador");
                string forfeitMessage = $"{forfeitPlayerName} se rindió.";

                return new SubmitBattleActionResult {
                    Accepted = true,
                    TurnResolved = true,
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = forfeitMessage } },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (!IsValidActionPayload(battle, userId, action, moveName, targetSlot, out string validationError)) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = validationError } },
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
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = "No hay rival asignado." } },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            // Si esta es una acción de cambio forzoso (por debilitamiento), procesarla inmediatamente
            bool isRequiredSwitch = battle.RequiredSwitchByUserId.Contains(userId) && action == BattleAction.Switch;
            if (isRequiredSwitch) {
                var switchResult = new TurnResolutionResult();
                ExecuteSwitch(battle, userId, battle.PendingActionsByUserId[userId], switchResult);
                battle.PendingActionsByUserId.Remove(userId);

                // Verificar si aún quedan cambios requeridos pendientes para este usuario
                bool stillRequiresSwitch = battle.RequiredSwitchByUserId.Contains(userId);
                List<int> availableSwitchSlots = new List<int>();

                if (stillRequiresSwitch) {
                    var side = battle.GetSideForUser(userId);
                    if (side != null) {
                        for (int i = 0; i < side.Team.Count; i++) {
                            if (i != side.ActiveSlot && !side.Team[i].IsFainted()) {
                                availableSwitchSlots.Add(i);
                            }
                        }
                    }
                }

                return new SubmitBattleActionResult {
                    Accepted = true,
                    TurnResolved = false,
                    ReplaySteps = switchResult.StepBuilder.Build(),
                    WinnerUserId = battle.WinnerUserId,
                    RequiresSwitchSelection = stillRequiresSwitch,
                    AvailableSlotsForSwitch = availableSwitchSlots
                };
            }

            if (!battle.PendingActionsByUserId.ContainsKey(opponentUserId.Value)) {
                return new SubmitBattleActionResult {
                    Accepted = true,
                    TurnResolved = false,
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = "Acción recibida. Esperando al rival..." } },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            // Ya hay 2 acciones pendientes: resolver turno simultáneo
            int player2UserId = battle.Player2UserId ?? opponentUserId.Value;
            if (!battle.PendingActionsByUserId.TryGetValue(battle.PlayerUserId, out PendingBattleAction? actionP1)) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = "Falta la acción del jugador 1." } },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            if (!battle.PendingActionsByUserId.TryGetValue(player2UserId, out PendingBattleAction? actionP2)) {
                return new SubmitBattleActionResult {
                    Accepted = false,
                    TurnResolved = false,
                    ReplaySteps = new List<ReplayStep> { new ReplayStep { StepIndex = 0, Message = "Falta la acción del jugador 2." } },
                    WinnerUserId = battle.WinnerUserId
                };
            }

            var turnResult = ResolveSimultaneousTurn(battle, actionP1, actionP2, battle.PlayerUserId, player2UserId);

            battle.PendingActionsByUserId.Clear();
            battle.Turn++;

            // Verificar si el usuario actual necesita elegir un Pokémon de reemplazo
            bool requiresSwitch = battle.RequiredSwitchByUserId.Contains(userId);
            List<int> availableSlotsForSwitch = new List<int>();

            if (requiresSwitch) {
                var side = battle.GetSideForUser(userId);
                if (side != null) {
                    for (int i = 0; i < side.Team.Count; i++) {
                        if (i != side.ActiveSlot && !side.Team[i].IsFainted()) {
                            availableSlotsForSwitch.Add(i);
                        }
                    }
                }
            }

            return new SubmitBattleActionResult {
                Accepted = true,
                TurnResolved = true,
                ReplaySteps = turnResult.StepBuilder.Build(),
                WinnerUserId = battle.WinnerUserId,
                RequiresSwitchSelection = requiresSwitch,
                AvailableSlotsForSwitch = availableSlotsForSwitch
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

    // Helper para traducir nombres de estadísticas al español
    private string TranslateStatName(string stat) {
        return stat switch {
            "Attack" => "Ataque",
            "Defense" => "Defensa",
            "SpecialAttack" => "Ataque Especial",
            "SpecialDefense" => "Defensa Especial",
            "Speed" => "Velocidad",
            "Accuracy" => "Precisión",
            "Evasion" => "Evasión",
            _ => stat
        };
    }

    // Helper para generar mensaje de cambio de estadística
    private string GetStatChangeMessage(string pokemonName, string stat, int change) {
        string statNameEs = TranslateStatName(stat);
        int absChange = Math.Abs(change);

        if (change > 0) {
            // Aumento
            return absChange >= 2 
                ? $"{statNameEs} de {pokemonName} ha subido mucho."
                : $"{statNameEs} de {pokemonName} ha subido.";
        } else {
            // Reducción
            return absChange >= 2 
                ? $"{statNameEs} de {pokemonName} ha bajado mucho."
                : $"{statNameEs} de {pokemonName} ha bajado.";
        }
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

    // Helper para añadir mensaje de efectividad según el multiplicador
    private void AddEffectivenessMessage(
        TurnResolutionResult result,
        double effectiveness,
        Dictionary<string, object> args) {

        if (effectiveness == 0) {
            result.StepBuilder.AddMessageStep("No afecta...");
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.NoEffect, args));
        } else if (effectiveness < 1.0) {
            result.StepBuilder.AddMessageStep("No es muy eficaz...");
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.NotVeryEffective, args));
        } else if (effectiveness > 1.0) {
            result.StepBuilder.AddMessageStep("¡Es muy eficaz!");
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.SuperEffective, args));
        }
    }

    // ========== REPLAY STEP HELPERS ==========

    /// <summary>
    /// Crea un step de ataque usado (mensaje + AttackEvent)
    /// </summary>
    private void AddAttackUsedStep(
        TurnResolutionResult result,
        string message,
        StructuredBattleMessage? structuredMessage,
        AttackEvent attackEvent) {

        result.StepBuilder.AddStep(
            textMessage: message,
            structuredMessage: structuredMessage,
            singleEvent: attackEvent
        );
    }

    /// <summary>
    /// Crea un step de cambio de HP (mensaje + HpChangeEvent)
    /// </summary>
    private void AddHpChangeStep(
        TurnResolutionResult result,
        string message,
        StructuredBattleMessage? structuredMessage,
        HpChangeEvent hpEvent) {

        result.StepBuilder.AddStep(
            textMessage: message,
            structuredMessage: structuredMessage,
            singleEvent: hpEvent
        );
    }

    /// <summary>
    /// Crea un step de mensaje estructurado (crítico, efectividad, etc.)
    /// </summary>
    private void AddMessageStep(
        TurnResolutionResult result,
        string? message,
        StructuredBattleMessage? structuredMessage) {

        if (message != null || structuredMessage != null) {
            result.StepBuilder.AddStep(
                textMessage: message,
                structuredMessage: structuredMessage
            );
        }
    }

    /// <summary>
    /// Crea un step de cambio de estadística (mensaje + StatStageChangeEvent)
    /// </summary>
    private void AddStatChangeStep(
        TurnResolutionResult result,
        string message,
        StatStageChangeEvent statEvent) {

        result.StepBuilder.AddStep(
            textMessage: message,
            singleEvent: statEvent
        );
    }

    /// <summary>
    /// Crea un step genérico con mensaje y/o evento
    /// </summary>
    private void AddGenericStep(
        TurnResolutionResult result,
        string? message,
        BattleEvent? battleEvent = null,
        StructuredBattleMessage? structuredMessage = null) {

        result.StepBuilder.AddStep(
            textMessage: message,
            structuredMessage: structuredMessage,
            singleEvent: battleEvent
        );
    }

    // ========== FIN REPLAY STEP HELPERS ==========

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

        // Si el usuario necesita hacer un cambio forzoso, solo se permite Switch
        bool requiresForcedSwitch = battle.RequiredSwitchByUserId.Contains(userId);
        if (requiresForcedSwitch && action != BattleAction.Switch) {
            errorMessage = "Debes elegir un Pokémon de reemplazo.";
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
            string player1Name = battle.PlayerUserName ?? "Jugador 1";
            string forfeitMsg = $"{player1Name} se rindió.";
            var forfeitEvent = new Networking.Messages.Battle.BattleEndEvent {
                Message = forfeitMsg,
                Winner = "opponent",
                WinnerUserId = player2UserId
            };

            // ReplayStep
            result.StepBuilder.AddStep(
                textMessage: forfeitMsg,
                singleEvent: forfeitEvent
            );

            return result;
        }
        if (player2Action.Action == BattleAction.Forfeit) {
            battle.WinnerUserId = player1UserId;
            string player2Name = battle.Player2UserName ?? "Jugador 2";
            string forfeitMsg = $"{player2Name} se rindió.";
            var forfeitEvent = new Networking.Messages.Battle.BattleEndEvent {
                Message = forfeitMsg,
                Winner = "player",
                WinnerUserId = player1UserId
            };

            // ReplayStep
            result.StepBuilder.AddStep(
                textMessage: forfeitMsg,
                singleEvent: forfeitEvent
            );

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
            string winnerName = battle.GetWinnerName() ?? "Desconocido";
            string winMsg = $"{winnerName} ganó la batalla.";
            var winEvent = new Networking.Messages.Battle.BattleEndEvent {
                Message = winMsg,
                Winner = winner,
                WinnerUserId = battle.WinnerUserId
            };

            // ReplayStep
            result.StepBuilder.AddStep(
                textMessage: winMsg,
                singleEvent: winEvent
            );
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
            string errorMsg = "No se pudo realizar el cambio de Pokémon.";

            // Legacy
            result.StepBuilder.AddMessageStep(errorMsg);
            result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = errorMsg
             });

            // ReplayStep
            result.StepBuilder.AddStep(
                textMessage: errorMsg,
                singleEvent: new Networking.Messages.Battle.MessageEvent {
                    Message = errorMsg
                }
            );

            return;
        }

        // Remover el userId de cambios requeridos si estaba presente
        battle.RequiredSwitchByUserId.Remove(userId);

        var active = mySide.GetActivePokemon();
        string name = active?.GetDisplayName() ?? "Pokémon";
        string side = battle.PlayerUserId == userId ? "player" : "opponent";
        string message = $"Cambio realizado: entra {name}.";

        var switchEvent = new Networking.Messages.Battle.SwitchEvent {
            Message = message,
            Side = side,
            PreviousActiveSlot = previousSlot,
            NewActiveSlot = action.TargetSlot.Value,
            NewPokemonName = name,
            IsAutomatic = false
        };

        // ReplayStep - switch es una acción atómica con su evento
        result.StepBuilder.AddStep(
            textMessage: message,
            singleEvent: switchEvent
        );
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
            result.StepBuilder.AddMessageStep(msg);
            result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = msg
             });
            return;
        }

        var attackerId = CreatePokemonIdentifier(battle, userId, attacker);
        int? rivalUserId = battle.GetOpponentUserId(userId);
        var defenderId = rivalUserId.HasValue ? CreatePokemonIdentifier(battle, rivalUserId.Value, defender) : null;

        // Verificar si el Pokémon puede atacar (estados como Freeze, Sleep, Paralysis, Confusion)
        var (canAttack, statusMessage, resultMessage) = attacker.CanAttack();

        // Si hay mensaje de estado, mostrarlo primero
        if (statusMessage != null) {
            result.StepBuilder.AddMessageStep(statusMessage);
        }

        // Si no puede atacar, mostrar mensaje de resultado y terminar
        if (!canAttack) {
            if (resultMessage != null) {
                result.StepBuilder.AddMessageStep(resultMessage);
            }

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
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.FrozenSolid, actorArgs));
            } else if (attacker.Status == Enum.PokeStatus.Sleep) {
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.FastAsleep, actorArgs));
            } else if (attacker.Status == Enum.PokeStatus.Paralysis) {
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.ParalyzedCantMove, actorArgs));
            } else if (attacker.HasSecondaryStatus(Enum.PokeSecondaryStatus.Confuse)) {
                // Confusion self-hit: NO emitir attack_used
                // El daño ya fue aplicado en CanAttack()
                var confusionArgs = actorArgs;
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.ConfusionSelfHit, confusionArgs));
            }

            if (defenderId != null) {
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.AttackEvent {
                    Message = resultMessage ?? statusMessage ?? "No puede atacar",
                    Attacker = attackerId,
                    Defender = defenderId,
                    MoveName = move?.Name ?? action.MoveName ?? "Unknown",
                    Hit = false,
                    Blocked = true,
                    BlockReason = blockReason
                });
            }

            // Si se golpeó a sí mismo por confusión, generar evento de HP
            if (attacker.HasSecondaryStatus(Enum.PokeSecondaryStatus.Confuse) && resultMessage != null) {
                // El daño ya fue aplicado en CanAttack(), solo necesitamos el evento
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.HpChangeEvent {
                    Message = resultMessage,
                    Target = attackerId,
                    BeforeHp = attacker.CurrentHp, // Ya se aplicó el daño
                    AfterHp = attacker.CurrentHp,
                    MaxHp = attacker.MaxHp,
                    Amount = 0, // No tenemos el valor exacto aquí
                    Cause = "confusion"
                });
            }

            return;
        }

        // Puede atacar - mostrar mensaje de resultado si existe (ej: "ya no está confundido", "se descongeló")
        if (resultMessage != null) {
            result.StepBuilder.AddMessageStep(resultMessage);

            var actorArgs = CreateActorArgs(battle, userId, attacker);

            if (resultMessage.Contains("despertado") || resultMessage.Contains("despierto")) {
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.WokeUp, actorArgs));
            } else if (resultMessage.Contains("descongelado")) {
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.Thawed, actorArgs));
            } else if (resultMessage.Contains("ya no está confundido")) {
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.ConfusionEnd, actorArgs));
            }
        }

        var selectedMove = attacker.Movements
            .FirstOrDefault(m => string.Equals(m.Name, action.MoveName, StringComparison.OrdinalIgnoreCase));

        if (selectedMove == null || !selectedMove.HasPpAvailable()) {
            string msg = $"{attacker.GetDisplayName()} no pudo usar el movimiento.";
            result.StepBuilder.AddMessageStep(msg);
            result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = msg
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

        MovementResult movementResult = selectedMove.ExecuteMovement(attacker, defender);

        // Manejar caso de fallo por falta de PP
        if (movementResult.FailedByNoPp) {
            string noPpMsg = $"{attacker.GetDisplayName()} no tiene PP para usar {selectedMove.Name}.";
            result.StepBuilder.AddMessageStep(noPpMsg);

            var noPpArgs = CreateActorArgs(battle, userId, attacker);
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.OutOfPp, noPpArgs));

            result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = noPpMsg
             });
            return;
        }

        // Manejar caso de fallo por precisión
        if (movementResult.FailedByAccuracy) {
            string missMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}, ¡pero falla!";
            result.StepBuilder.AddMessageStep(missMsg);

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackUsed, attackArgs));

            if (rivalUserId.HasValue) {
                var missArgs = CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender);
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackMissed, missArgs));
            }

            if (defenderId != null) {
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.AttackEvent {
                    Message = missMsg,
                    Attacker = attackerId,
                    Defender = defenderId,
                    MoveName = selectedMove.Name,
                    Hit = false,
                    Blocked = false
                });
            }
            return;
        }

        // Manejar caso de fallo porque el objetivo ya tiene un estado alterado
        if (movementResult.FailedByExistingStatus) {
            string failMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}, ¡pero falla!";
            result.StepBuilder.AddMessageStep(failMsg);

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackUsed, attackArgs));

            if (rivalUserId.HasValue) {
                var failArgs = CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender);
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackMissed, failArgs));
            }

            if (defenderId != null) {
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.AttackEvent {
                    Message = failMsg,
                    Attacker = attackerId,
                    Defender = defenderId,
                    MoveName = selectedMove.Name,
                    Hit = false,
                    Blocked = false
                });
            }
            return;
        }

        int hpAfter = defender.CurrentHp;
        int hpAfterAttacker = attacker.CurrentHp;
        int damage = movementResult.Damage;

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

        int healing = movementResult.Healing;

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
                    result.StepBuilder.AddMessageStep(mistBlockMsg);

                    // Mensaje estructurado
                    if (rivalUserId.HasValue) {
                        var mistArgs = CreateActorArgs(battle, rivalUserId.Value, defender);
                        result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.StatProtected, mistArgs));
                    }

                    result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = mistBlockMsg
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
            string attackMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}.";
            result.StepBuilder.AddMessageStep(attackMsg);

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackUsed, attackArgs));

            if (rivalUserId.HasValue) {
                // Añadir mensaje de efectividad si aplica
                AddEffectivenessMessage(result, movementResult.TypeEffectiveness, CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender));

                // Añadir mensaje de crítico si aplica
                if (movementResult.IsCritical) {
                    var critArgs = CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender);
                    result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.CriticalHit, critArgs));
                }

                // Mensaje de daño recibido (sin números)
                result.StepBuilder.AddMessageStep($"{defender.GetDisplayName()} ha recibido daño.");

                // Si es un movimiento multi-golpe, indicar el número de golpes
                if (movementResult.HitCount > 1) {
                    result.StepBuilder.AddMessageStep($"¡Golpeó {movementResult.HitCount} veces!");
                }

                var damageArgs = new Dictionary<string, object>(CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender)) {
                    { "damage", damage }
                };
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.DamageDealt, damageArgs));

                // Mensaje de curación (sin números)
                result.StepBuilder.AddMessageStep($"{attacker.GetDisplayName()} absorbe PS.");

                var drainArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                    { "amount", healing }
                };
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.DrainHp, drainArgs));
            }

            if (defenderId != null) {
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.AttackEvent {
                    Message = attackMsg,
                    Attacker = attackerId,
                    Defender = defenderId,
                    MoveName = selectedMove.Name,
                    Hit = true,
                    Blocked = false
                });

                // Evento de daño al defensor
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.HpChangeEvent {
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
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.HpChangeEvent {
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
            string attackMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}.";
            result.StepBuilder.AddMessageStep(attackMsg);

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackUsed, attackArgs));

            if (rivalUserId.HasValue) {
                // Añadir mensaje de efectividad si aplica
                AddEffectivenessMessage(result, movementResult.TypeEffectiveness, CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender));

                // Añadir mensaje de crítico si aplica
                if (movementResult.IsCritical) {
                    var critArgs = CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender);
                    result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.CriticalHit, critArgs));
                }

                // Mensaje de daño recibido (sin números)
                result.StepBuilder.AddMessageStep($"{defender.GetDisplayName()} ha recibido daño.");

                // Si es un movimiento multi-golpe, indicar el número de golpes
                if (movementResult.HitCount > 1) {
                    result.StepBuilder.AddMessageStep($"¡Golpeó {movementResult.HitCount} veces!");
                }

                var damageArgs = new Dictionary<string, object>(CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender)) {
                    { "damage", damage }
                };
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.DamageDealt, damageArgs));
            }

            if (defenderId != null) {
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.AttackEvent {
                    Message = attackMsg,
                    Attacker = attackerId,
                    Defender = defenderId,
                    MoveName = selectedMove.Name,
                    Hit = true,
                    Blocked = false
                });

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.HpChangeEvent {
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
                    result.StepBuilder.AddMessageStep(statMsg);

                    // Mensaje estructurado para cambios de stats
                    var statArgs = new Dictionary<string, object>(CreateActorArgs(battle, rivalUserId.Value, defender)) {
                        { "stat", Utils.TextNormalizer.ToSnakeCase(stat) },
                        { "stages", Math.Abs(change) }
                    };
                    string statCode = change > 0 ? BattleMessageCode.StatRose : BattleMessageCode.StatFell;
                    result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(statCode, statArgs));

                    result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.StatStageChangeEvent {
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
            string healMsg = $"{attacker.GetDisplayName()} usa {selectedMove.Name}.";
            result.StepBuilder.AddMessageStep(healMsg);
            result.StepBuilder.AddMessageStep($"{attacker.GetDisplayName()} recupera PS.");

            // Mensajes estructurados
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackUsed, attackArgs));

            var healArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "amount", healing }
            };
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.HpRestored, healArgs));

            result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.AttackEvent {
                Message = healMsg,
                Attacker = attackerId,
                Defender = attackerId, // El defensor es el mismo atacante
                MoveName = selectedMove.Name,
                Hit = true,
                Blocked = false
            });

            result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.HpChangeEvent {
                Message = $"{attacker.GetDisplayName()} recupera PS.",
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
            result.StepBuilder.AddMessageStep(moveMsg);

            // Mensaje estructurado de uso de ataque
            var attackArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                { "move", Utils.TextNormalizer.ToSnakeCase(selectedMove.Name) }
            };
            result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.AttackUsed, attackArgs));

            result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.AttackEvent {
                Message = moveMsg,
                Attacker = attackerId,
                Defender = defenderId,
                MoveName = selectedMove.Name,
                Hit = true,
                Blocked = false
            });

            // Caso especial: Drenadoras (Leech Seed)
            if (movementResult.AppliedSeeded && rivalUserId.HasValue) {
                string seededMsg = $"{defender.GetDisplayName()} ha sido infectado.";
                result.StepBuilder.AddMessageStep(seededMsg);

                var seededArgs = CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender);
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.Seeded, seededArgs));

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = seededMsg
                 });
            }
            // Inmunidad a Drenadoras (tipo Planta)
            else if (movementResult.ImmuneToSeeded && rivalUserId.HasValue) {
                string immuneMsg = $"No afecta a {defender.GetDisplayName()}.";
                result.StepBuilder.AddMessageStep(immuneMsg);

                var immuneArgs = CreateActorTargetArgs(battle, userId, attacker, rivalUserId.Value, defender);
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.NoEffect, immuneArgs));

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = immuneMsg
                 });
            }

            // Caso especial: Haze (ID 114) resetea todas las características
            // Emitir un mensaje general en lugar de listar cada cambio
            if (selectedMove.Id == 114 && (defenderStatChanges.Count > 0 || attackerStatChanges.Count > 0)) {
                string hazeMsg = "Las características de los Pokémon han vuelto a sus valores originales.";
                result.StepBuilder.AddMessageStep(hazeMsg);

                // Mensaje estructurado para reset de stats
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.StatsReset, new Dictionary<string, object>()));

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = hazeMsg
                 });
            }
            // Para otros movimientos, emitir eventos individuales de cambio de estadísticas
            else {
                // Emitir eventos de cambio de estadísticas del defensor si ocurrieron
                foreach (var (stat, change, newStage) in defenderStatChanges) {
                    string statMsg = GetStatChangeMessage(defender.GetDisplayName(), stat, change);
                    result.StepBuilder.AddMessageStep(statMsg);

                    result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.StatStageChangeEvent {
                        Message = statMsg,
                        Target = defenderId,
                        Stat = TranslateStatName(stat),
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
                string statMsg = GetStatChangeMessage(attacker.GetDisplayName(), stat, change);
                result.StepBuilder.AddMessageStep(statMsg);

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.StatStageChangeEvent {
                    Message = statMsg,
                    Target = attackerId,
                    Stat = TranslateStatName(stat),
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
                result.StepBuilder.AddMessageStep(forcedSwitchMsg);

                string rivalSideStr = battle.PlayerUserId == userId ? "opponent" : "player";
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.SwitchEvent {
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
                result.StepBuilder.AddMessageStep(mistMsg);

                // Mensaje estructurado
                var mistArgs = new Dictionary<string, object>(CreateActorArgs(battle, userId, attacker)) {
                    { "turns", 5 }
                };
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.MistStart, mistArgs));

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = mistMsg
                 });
            }
            // Light Screen (ID 113): Reduce el daño de ataques especiales enemigos a la mitad durante 5 turnos
            else if (selectedMove.Id == 113) {
                mySide.LightScreenTurnsRemaining = 5;
                string lightScreenMsg = $"¡Pantalla de Luz reduce el daño de ataques especiales!";
                result.StepBuilder.AddMessageStep(lightScreenMsg);

                // Mensaje estructurado
                var lightScreenArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) },
                    { "turns", 5 }
                };
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.LightScreenStart, lightScreenArgs));

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = lightScreenMsg
                 });
            }
            // Reflect (ID 115): Reduce el daño de ataques físicos enemigos a la mitad durante 5 turnos
            else if (selectedMove.Id == 115) {
                mySide.ReflectTurnsRemaining = 5;
                string reflectMsg = $"¡Reflejo reduce el daño de ataques físicos!";
                result.StepBuilder.AddMessageStep(reflectMsg);

                // Mensaje estructurado
                var reflectArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) },
                    { "turns", 5 }
                };
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.ReflectStart, reflectArgs));

                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.MessageEvent { Message = reflectMsg
                 });
            }
        }

        if (defender.IsFainted()) {
            string faintMsg = $"{defender.GetDisplayName()} se debilitó.";
            result.StepBuilder.AddMessageStep(faintMsg);

            if (defenderId != null) {
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.FaintEvent {
                    Message = faintMsg,
                    Target = defenderId
                });
            }

            // Marcar que el rival necesita elegir un Pokémon de reemplazo
            // en lugar de hacer cambio automático
            if (rivalUserId.HasValue && rivalSide != null && !rivalSide.IsDefeated()) {
                battle.RequiredSwitchByUserId.Add(rivalUserId.Value);
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
                var mistArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) }
                };
                var mistStructuredMsg = BattleMessageBuilder.Create(BattleMessageCode.MistEnd, mistArgs);
                var mistEvent = new Networking.Messages.Battle.MessageEvent {
                    Message = mistEndMsg
                };

                // ReplayStep
                result.StepBuilder.AddStep(
                    textMessage: mistEndMsg,
                    structuredMessage: mistStructuredMsg,
                    singleEvent: mistEvent
                );
            }
        }

        // Light Screen
        if (side.LightScreenTurnsRemaining > 0) {
            side.LightScreenTurnsRemaining--;
            if (side.LightScreenTurnsRemaining == 0) {
                string lightScreenEndMsg = "¡El efecto de Pantalla de Luz se disipó!";
                var lightScreenArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) }
                };
                var lightScreenStructuredMsg = BattleMessageBuilder.Create(BattleMessageCode.LightScreenEnd, lightScreenArgs);
                var lightScreenEvent = new Networking.Messages.Battle.MessageEvent {
                    Message = lightScreenEndMsg
                };

                // ReplayStep
                result.StepBuilder.AddStep(
                    textMessage: lightScreenEndMsg,
                    structuredMessage: lightScreenStructuredMsg,
                    singleEvent: lightScreenEvent
                );
            }
        }

        // Reflect
        if (side.ReflectTurnsRemaining > 0) {
            side.ReflectTurnsRemaining--;
            if (side.ReflectTurnsRemaining == 0) {
                string reflectEndMsg = "¡El efecto de Reflejo se disipó!";
                var reflectArgs = new Dictionary<string, object> {
                    { "owner", GetOwnerName(battle, userId) }
                };
                var reflectStructuredMsg = BattleMessageBuilder.Create(BattleMessageCode.ReflectEnd, reflectArgs);
                var reflectEvent = new Networking.Messages.Battle.MessageEvent {
                    Message = reflectEndMsg
                };

                // ReplayStep
                result.StepBuilder.AddStep(
                    textMessage: reflectEndMsg,
                    structuredMessage: reflectStructuredMsg,
                    singleEvent: reflectEvent
                );
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
            if (hpBefore != hpAfter) {
                string cause = pokemon.Status switch {
                    Enum.PokeStatus.Burn => "burn",
                    Enum.PokeStatus.Poison => "poison",
                    Enum.PokeStatus.BadlyPoisoned => "badly_poisoned",
                    _ => "status_effect"
                };

                int damage = Math.Max(0, hpBefore - hpAfter);
                var hpChangeEvent = new Networking.Messages.Battle.HpChangeEvent {
                    Message = effect,
                    Target = pokemonId,
                    BeforeHp = hpBefore,
                    AfterHp = hpAfter,
                    MaxHp = pokemon.MaxHp,
                    Amount = -damage,
                    Cause = cause
                };

                // ReplayStep
                result.StepBuilder.AddStep(
                    textMessage: effect,
                    singleEvent: hpChangeEvent
                );
            } else {
                // Solo mensaje sin cambio de HP (legacy)
                result.StepBuilder.AddMessageStep(effect);
            }

            // Verificar si se debilitó por el efecto de estado
            if (pokemon.IsFainted()) {
                string faintMsg = $"{pokemon.GetDisplayName()} se debilitó.";
                var faintEvent = new Networking.Messages.Battle.FaintEvent {
                    Message = faintMsg,
                    Target = pokemonId
                };

                // ReplayStep
                result.StepBuilder.AddStep(
                    textMessage: faintMsg,
                    singleEvent: faintEvent
                );

                // Marcar que el jugador necesita elegir un Pokémon de reemplazo
                if (!side.IsDefeated()) {
                    battle.RequiredSwitchByUserId.Add(userId);
                }
                return; // No aplicar efectos secundarios si está debilitado
            }
        }

        // Aplicar efectos secundarios (leech seed, etc.)
        hpBefore = pokemon.CurrentHp;
        string? secondaryEffect = pokemon.ApplyEndOfTurnSecondaryStatusEffect();
        hpAfter = pokemon.CurrentHp;

        if (secondaryEffect != null) {
            result.StepBuilder.AddMessageStep(secondaryEffect);

            if (hpBefore != hpAfter && pokemon.HasSecondaryStatus(Enum.PokeSecondaryStatus.Seeded)) {
                int damage = Math.Max(0, hpBefore - hpAfter);

                // Obtener el Pokémon activo del lado contrario para curarlo
                int? opponentUserId = battle.GetOpponentUserId(userId);
                BattleSide? opponentSide = opponentUserId.HasValue ? battle.GetSideForUser(opponentUserId.Value) : null;
                PokemonBattle? opponentActivePokemon = opponentSide?.GetActivePokemon();

                // Mensaje estructurado para el drenaje de Drenadoras
                var drainArgs = new Dictionary<string, object> {
                    { "target", pokemon.GetDisplayName() },
                    { "amount", damage }
                };
                if (opponentActivePokemon != null) {
                    drainArgs["source"] = opponentActivePokemon.GetDisplayName();
                }
                result.StepBuilder.AddStructuredStep(null, BattleMessageBuilder.Create(BattleMessageCode.SeededDrain, drainArgs));

                // Crear evento de HP para el Pokémon afectado (pierde vida)
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.HpChangeEvent {
                    Message = $"{pokemon.GetDisplayName()} pierde {damage} PS por Drenadoras.",
                    Target = pokemonId,
                    BeforeHp = hpBefore,
                    AfterHp = hpAfter,
                    MaxHp = pokemon.MaxHp,
                    Amount = -damage,
                    Cause = "leech_seed",
                    SourcePokemon = opponentActivePokemon != null && opponentUserId.HasValue
                        ? CreatePokemonIdentifier(battle, opponentUserId.Value, opponentActivePokemon)
                        : null
                });

                // Curar al Pokémon activo del lado contrario (si existe y no está debilitado)
                if (opponentActivePokemon != null && !opponentActivePokemon.IsFainted() && opponentUserId.HasValue) {
                    int hpBeforeHeal = opponentActivePokemon.CurrentHp;
                    opponentActivePokemon.Heal(damage);
                    int hpAfterHeal = opponentActivePokemon.CurrentHp;
                    int actualHealing = hpAfterHeal - hpBeforeHeal;

                    if (actualHealing > 0) {
                        var sourceId = CreatePokemonIdentifier(battle, opponentUserId.Value, opponentActivePokemon);

                        result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.HpChangeEvent {
                            Message = $"{opponentActivePokemon.GetDisplayName()} recupera {actualHealing} PS.",
                            Target = sourceId,
                            BeforeHp = hpBeforeHeal,
                            AfterHp = hpAfterHeal,
                            MaxHp = opponentActivePokemon.MaxHp,
                            Amount = actualHealing,
                            Cause = "leech_seed",
                            SourcePokemon = pokemonId
                        });
                    }
                }
            }

            // Verificar si se debilitó por el efecto secundario
            if (pokemon.IsFainted()) {
                string faintMsg = $"{pokemon.GetDisplayName()} se debilitó.";
                result.StepBuilder.AddMessageStep(faintMsg);
                result.StepBuilder.AddEventToLastStep(new Networking.Messages.Battle.FaintEvent {
                    Message = faintMsg,
                    Target = pokemonId
                });

                // Marcar que el jugador necesita elegir un Pokémon de reemplazo
                if (!side.IsDefeated()) {
                    battle.RequiredSwitchByUserId.Add(userId);
                }
            }
        }
    }
}