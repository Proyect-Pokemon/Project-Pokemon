using ProjectPokemon.Models.WebSocket;

namespace ProjectPokemon.Models.Battle;

// Representa una sesión de batalla activa en memoria
public class BattleSession {
    public string BattleId { get; set; } = Guid.NewGuid().ToString();
    public required BattleSide PlayerSide { get; set; }
    public required BattleSide OpponentSide { get; set; }
    public int Turn { get; set; } = 1;
    public List<string> BattleLog { get; set; } = new();
    public string? WinnerSide { get; set; } = null;

    // Conectar usuario con la batalla
    public required int PlayerUserId { get; set; }
    public string? PlayerConnectionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    // Genera el snapshot actual de la batalla para enviar al cliente
    public BattleSnapshot CreateSnapshot() {
        return new BattleSnapshot {
            BattleId = BattleId,
            Turn = Turn,
            PlayerSide = new BattleSideSnapshot {
                Team = PlayerSide.Team.Select(p => CreatePokemonSnapshot(p, isPlayerSide: true)).ToList(),
                ActiveSlot = PlayerSide.ActiveSlot
            },
            OpponentSide = new BattleSideSnapshot {
                Team = OpponentSide.Team.Select(p => CreatePokemonSnapshot(p, isPlayerSide: false)).ToList(),
                ActiveSlot = OpponentSide.ActiveSlot
            }
        };
    }

    private PokemonSnapshot CreatePokemonSnapshot(PokemonBattle pokemon, bool isPlayerSide) {
        bool isActive = (isPlayerSide && PlayerSide.GetActivePokemon() == pokemon) ||
                        (!isPlayerSide && OpponentSide.GetActivePokemon() == pokemon);

        return new PokemonSnapshot {
            PokemonId = pokemon.PokemonId,
            Name = pokemon.Name,
            Nickname = pokemon.Nickname,
            Slot = pokemon.Slot,
            CurrentHp = pokemon.CurrentHp,
            MaxHp = pokemon.MaxHp,
            IsFainted = pokemon.IsFainted(),
            Status = pokemon.Status.ToString(),
            SpriteFront = null, // TODO: Obtener del Pokemon entity
            SpriteBack = null,  // TODO: Obtener del Pokemon entity
            Shiny = pokemon.Shiny,

            // Detalles completos solo si está activo
            Movements = isActive ? pokemon.Movements.Select(m => new MovementSnapshot {
                Name = m.Name,
                CurrentPp = m.CurrentPp,
                Pp = m.Pp,
                Type = m.Type.ToString()
            }).ToList() : null,
            Attack = isActive ? pokemon.GetModifiedStat(Enum.StatType.Attack) : null,
            Defense = isActive ? pokemon.GetModifiedStat(Enum.StatType.Defense) : null,
            SpecialAttack = isActive ? pokemon.GetModifiedStat(Enum.StatType.SpecialAttack) : null,
            SpecialDefense = isActive ? pokemon.GetModifiedStat(Enum.StatType.SpecialDefense) : null,
            Speed = isActive ? pokemon.GetModifiedStat(Enum.StatType.Speed) : null
        };
    }

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