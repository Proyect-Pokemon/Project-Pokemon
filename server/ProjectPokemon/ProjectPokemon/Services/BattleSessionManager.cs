using ProjectPokemon.Models.Battle;
using System.Collections.Concurrent;

namespace ProjectPokemon.Services;

// Gestiona las sesiones de batalla activas en memoria
public class BattleSessionManager {
    private readonly ConcurrentDictionary<string, BattleSession> _activeBattles = new();

    public BattleSession? GetBattle(string battleId) {
        _activeBattles.TryGetValue(battleId, out var battle);
        return battle;
    }

    public BattleSession CreateBattle(BattleSession session) {
        _activeBattles[session.BattleId] = session;
        return session;
    }

    public bool RemoveBattle(string battleId) {
        return _activeBattles.TryRemove(battleId, out _);
    }

    public BattleSession? GetBattleByUserId(int userId) {
        return _activeBattles.Values.FirstOrDefault(b => b.PlayerUserId == userId);
    }

    public BattleSession? GetBattleByConnectionId(string connectionId) {
        return _activeBattles.Values.FirstOrDefault(b => b.PlayerConnectionId == connectionId);
    }
}