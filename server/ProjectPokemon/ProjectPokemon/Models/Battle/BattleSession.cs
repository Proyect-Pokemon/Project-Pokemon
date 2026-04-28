using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Battle;

// Representa una sesión de batalla activa en memoria
public class BattleSession {
    public string BattleId { get; set; } = Guid.NewGuid().ToString();
    public required BattleSide PlayerSide { get; set; }
    public required BattleSide OpponentSide { get; set; }
    public int Turn { get; set; } = 1;
    public List<string> BattleLog { get; set; } = new();
    public string? WinnerSide { get; set; } = null;

    // Player1 = PlayerSide, Player2 = OpponentSide
    public required int PlayerUserId { get; set; }
    public int? Player2UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Verifica si la batalla ha terminado
    public bool IsBattleOver() {
        if (PlayerSide.IsDefeated()) {
            WinnerSide = "opponent";
            return true;
        }
        if (OpponentSide.IsDefeated()) {
            WinnerSide = "player";
            return true;
        }
        return false;
    }
}

// Representa un lado de la batalla (jugador u oponente)
public class BattleSide {
    public List<PokemonBattle> Team { get; set; } = new();
    public int ActiveSlot { get; set; } = 0; // 0-5

    public PokemonBattle? GetActivePokemon() {
        if (ActiveSlot < 0 || ActiveSlot >= Team.Count) return null;
        return Team[ActiveSlot];
    }

    // Cambia el Pokémon activo
    public bool SwitchPokemon(int newSlot) {
        if (newSlot < 0 || newSlot >= Team.Count) return false;
        if (Team[newSlot].IsFainted()) return false;

        ActiveSlot = newSlot;
        return true;
    }

    // Verifica si todos los Pokémon están debilitados
    public bool IsDefeated() {
        return Team.All(p => p.IsFainted());
    }

    // Obtiene el primer Pokémon no debilitado
    public int? GetFirstNonFaintedSlot() {
        for (int i = 0; i < Team.Count; i++) {
            if (!Team[i].IsFainted()) return i;
        }
        return null;
    }
}
