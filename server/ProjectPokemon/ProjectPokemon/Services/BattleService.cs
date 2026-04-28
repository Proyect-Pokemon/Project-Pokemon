using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Battle;

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
}
