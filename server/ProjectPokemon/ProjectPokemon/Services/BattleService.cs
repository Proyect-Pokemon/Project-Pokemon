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

    public class SubmitBattleActionResult
    {
        public bool Accepted { get; set; }
        public bool TurnResolved { get; set; }
        public List<string> Messages { get; set; } = new();
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
            .Include(pt => pt.Movement2)
            .Include(pt => pt.Movement3)
            .Include(pt => pt.Movement4)
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
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Movement2)
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Movement3)
            .Include(t => t.PokemonsTeam)
                .ThenInclude(pt => pt.Movement4)
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

            List<string> turnMessages = ResolveSimultaneousTurn(battle, actionP1, actionP2, battle.PlayerUserId, player2UserId);

            battle.PendingActionsByUserId.Clear();
            battle.Turn++;

            return new SubmitBattleActionResult {
                Accepted = true,
                TurnResolved = true,
                Messages = turnMessages,
                WinnerUserId = battle.WinnerUserId
            };
        }
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

    private List<string> ResolveSimultaneousTurn(
        BattleSession battle,
        PendingBattleAction player1Action,
        PendingBattleAction player2Action,
        int player1UserId,
        int player2UserId) {

        var logs = new List<string>();

        // Rendirse tiene prioridad total y termina la batalla inmediatamente.
        if (player1Action.Action == BattleAction.Forfeit) {
            battle.WinnerUserId = player2UserId;
            logs.Add("El jugador 1 se rindió.");
            return logs;
        }
        if (player2Action.Action == BattleAction.Forfeit) {
            battle.WinnerUserId = player1UserId;
            logs.Add("El jugador 2 se rindió.");
            return logs;
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
                    ExecuteSwitch(battle, userId, action, logs);
                    break;

                case BattleAction.Attack:
                    ExecuteAttack(battle, userId, action, logs);
                    break;
            }

            battle.IsBattleOver();
        }

        return logs;
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

    private void ExecuteSwitch(BattleSession battle, int userId, PendingBattleAction action, List<string> logs) {
        BattleSide? mySide = battle.GetSideForUser(userId);
        if (mySide == null || !action.TargetSlot.HasValue) {
            return;
        }

        bool switched = mySide.SwitchPokemon(action.TargetSlot.Value);
        if (!switched) {
            logs.Add("No se pudo realizar el cambio de Pokémon.");
            return;
        }

        var active = mySide.GetActivePokemon();
        string name = active?.GetDisplayName() ?? "Pokémon";
        logs.Add($"Cambio realizado: entra {name}.");
    }

    private void ExecuteAttack(BattleSession battle, int userId, PendingBattleAction action, List<string> logs) {
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
            logs.Add($"{attacker.GetDisplayName()} está debilitado y no puede atacar.");
            return;
        }

        var move = attacker.Movements
            .FirstOrDefault(m => string.Equals(m.Name, action.MoveName, StringComparison.OrdinalIgnoreCase));

        if (move == null || !move.HasPpAvailable()) {
            logs.Add($"{attacker.GetDisplayName()} no pudo usar el movimiento.");
            return;
        }

        int hpBefore = defender.CurrentHp;
        move.ExecuteMovement(attacker, defender);
        int damage = Math.Max(0, hpBefore - defender.CurrentHp);

        logs.Add($"{attacker.GetDisplayName()} usa {move.Name}. Daño: {damage}.");

        if (defender.IsFainted()) {
            logs.Add($"{defender.GetDisplayName()} se debilitó.");

            int? autoSwitchSlot = rivalSide.GetFirstNonFaintedSlot();
            if (autoSwitchSlot.HasValue) {
                rivalSide.SwitchPokemon(autoSwitchSlot.Value);
                var newActive = rivalSide.GetActivePokemon();
                logs.Add($"Entra {newActive?.GetDisplayName() ?? "Pokémon"} automáticamente.");
            }
        }
    }
}
