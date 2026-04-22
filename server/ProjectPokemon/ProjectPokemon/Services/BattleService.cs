using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Battle;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Services;

// Servicio para crear y gestionar combates
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

    // Crea un nuevo combate cargando el equipo del usuario
    public async Task<BattleSession?> StartBattleAsync(int userId, int teamId, string connectionId) {
        // Validar que el equipo pertenece al usuario
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

        // Validar que el equipo tenga al menos un Pokémon
        if (team.PokemonsTeam.Count < 1) {
            _logger.LogWarning($"El equipo {teamId} no tiene ni un Pokémon. No es un equipo válido.");
            return null;
        }

        // Cargar el equipo del jugador (ordenado por Slot)
        var playerTeam = team.PokemonsTeam
            .OrderBy(pt => pt.Slot)
            .Select(pt => new PokemonBattle(pt))
            .ToList();

        // Crear equipo rival temporal (por ahora 1 Pokémon fijo, "TODO: IA completa")
        var opponentPokemonEntity = await _context.PokemonTeams
            .Include(pt => pt.Pokemon)
            .Include(pt => pt.Nature)
            .Include(pt => pt.Movement1)
            .Include(pt => pt.Movement2)
            .Include(pt => pt.Movement3)
            .Include(pt => pt.Movement4)
            .FirstOrDefaultAsync(pt => pt.Id == 1); // TODO: Seleccionar equipo rival real

        if (opponentPokemonEntity == null) {
            _logger.LogError("No se encontró Pokémon rival");
            return null;
        }

        // Crear 6 copias del mismo Pokémon temporal (placeholder)
        var opponentTeam = Enumerable.Range(0, 6)
            .Select(i => {
                var poke = new PokemonBattle(opponentPokemonEntity);
                // Modificar slot para que sean únicos
                typeof(PokemonBattle).GetProperty("Slot")!.SetValue(poke, i);
                return poke;
            })
            .ToList();

        // Crear la sesión de batalla
        var session = new BattleSession {
            PlayerUserId = userId,
            PlayerConnectionId = connectionId,
            PlayerSide = new BattleSide {
                Team = playerTeam,
                ActiveSlot = 0
            },
            OpponentSide = new BattleSide {
                Team = opponentTeam,
                ActiveSlot = 0
            }
        };

        // Registrar la sesión
        _sessionManager.CreateBattle(session);

        _logger.LogInformation($"Batalla {session.BattleId} creada para usuario {userId}");

        return session;
    }
}