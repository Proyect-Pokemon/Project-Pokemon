using ProjectPokemon.Enum;
using ProjectPokemon.Networking.Messages.Battle;

namespace ProjectPokemon.Models.Battle;

// Representa una sesión de batalla activa en memoria
public class BattleSession
{
    public Guid BattleId { get; set; } = Guid.NewGuid();

    public required BattleSide PlayerSide { get; set; }
    public required BattleSide OpponentSide { get; set; }

    public BattleStatus Status { get; set; } = BattleStatus.WaitingForOpponent;

    public int Turn { get; set; } = 1;

    // Key = userId
    public Dictionary<int, PendingBattleAction> PendingActionsByUserId { get; } = new();

    public List<string> BattleLog { get; set; } = new();

    // UserId del ganador de la batalla
    public int? WinnerUserId { get; set; } = null;

    public object SyncRoot { get; } = new();

    // Player1 = PlayerSide, Player2 = OpponentSide
    public required int PlayerUserId { get; set; }
    public int? Player2UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsParticipant(int userId)
    {
        return PlayerUserId == userId || Player2UserId == userId;
    }

    public int? GetOpponentUserId(int userId)
    {
        if (PlayerUserId == userId) return Player2UserId;
        if (Player2UserId == userId) return PlayerUserId;

        return null;
    }

    public BattleSide? GetSideForUser(int userId)
    {
        if (PlayerUserId == userId) return PlayerSide;
        if (Player2UserId == userId) return OpponentSide;

        return null;
    }

    public BattleSide? GetOpponentSideForUser(int userId)
    {
        if (PlayerUserId == userId) return OpponentSide;
        if (Player2UserId == userId) return PlayerSide;

        return null;
    }

    // Verifica si la batalla ha terminado
    public bool IsBattleOver()
    {
        if (PlayerSide.IsDefeated())
        {
            WinnerUserId = Player2UserId;
            Status = BattleStatus.Finished;
            return true;
        }

        if (OpponentSide.IsDefeated())
        {
            WinnerUserId = PlayerUserId;
            Status = BattleStatus.Finished;
            return true;
        }

        return false;
    }
}

public class PendingBattleAction
{
    public required BattleAction Action { get; set; }
    public string? MoveName { get; set; }
    public int? TargetSlot { get; set; }
}

// Representa un lado de la batalla (jugador u oponente)
public class BattleSide
{
    public List<PokemonBattle> Team { get; set; } = new();

    public int ActiveSlot { get; set; } = 0; // 0-5

    public PokemonBattle? GetActivePokemon()
    {
        if (ActiveSlot < 0 || ActiveSlot >= Team.Count) return null;

        return Team[ActiveSlot];
    }

    // Cambia el Pokémon activo
    public bool SwitchPokemon(int newSlot)
    {
        if (newSlot < 0 || newSlot >= Team.Count) return false;

        if (newSlot == ActiveSlot) return false;

        if (Team[newSlot].IsFainted()) return false;

        ActiveSlot = newSlot;
        return true;
    }

    // Verifica si todos los Pokémon están debilitados
    public bool IsDefeated()
    {
        return Team.All(p => p.IsFainted());
    }

    // Obtiene el primer Pokémon no debilitado
    public int? GetFirstNonFaintedSlot()
    {
        for (int i = 0; i < Team.Count; i++)
        {
            if (!Team[i].IsFainted()) return i;
        }

        return null;
    }
}