namespace ProjectPokemon.Networking.Messages.Battle;

// Mensaje base para todas las comunicaciones de batalla
public abstract class BattleMessage : IMessage<BattleAction> {
    public MessageType Type => MessageType.Battle;
    public required BattleAction Action { get; set; }
}

// Cliente solicita iniciar una nueva batalla
public class StartBattleRequest : BattleMessage {
    public required int TeamId { get; set; }
    public string? OpponentUserId { get; set; } // null = IA, int = PvP
}

// Respuesta del servidor al iniciar batalla
public class StartBattleResponse : BattleMessage {
    public required string BattleId { get; set; }
    public required BattleSnapshot InitialState { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

// Mensaje del cliente para realizar una acción en la batalla
public class BattleActionRequest : BattleMessage {
    public required string BattleId { get; set; }
    public string? MoveName { get; set; }      // Para acción Attack
    public int? TargetSlot { get; set; }       // Para acción Switch (0-5)
}

// Actualización del estado de la batalla enviada por el servidor
public class BattleStateUpdate : BattleMessage {
    public required BattleSnapshot Battle { get; set; }
    public List<string> Messages { get; set; } = new();
    public bool RequiresSwitch { get; set; } = false;
    public string? WinnerSide { get; set; } = null; // "player" | "opponent" | null
}

// Snapshot completo del estado de la batalla
public class BattleSnapshot {
    public required string BattleId { get; set; }
    public required BattleSideSnapshot PlayerSide { get; set; }
    public required BattleSideSnapshot OpponentSide { get; set; }
    public int Turn { get; set; } = 1;
}

// Estado de un lado de la batalla
public class BattleSideSnapshot {
    public required List<PokemonSnapshot> Team { get; set; }
    public int ActiveSlot { get; set; } = 0;
}

// Snapshot de un Pokémon
public class PokemonSnapshot {
    public int PokemonId { get; set; }
    public required string Name { get; set; }
    public string? Nickname { get; set; }
    public int Slot { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public bool IsFainted { get; set; }
    public required string Status { get; set; }
    public string? SpriteFront { get; set; }
    public string? SpriteBack { get; set; }
    public bool Shiny { get; set; }
    public List<MovementSnapshot>? Movements { get; set; }
    public int? Attack { get; set; }
    public int? Defense { get; set; }
    public int? SpecialAttack { get; set; }
    public int? SpecialDefense { get; set; }
    public int? Speed { get; set; }
}

// Snapshot de un movimiento
public class MovementSnapshot {
    public required string Name { get; set; }
    public int CurrentPp { get; set; }
    public int MaxPp { get; set; }
    public required string Type { get; set; }
}
