namespace ProjectPokemon.Models.WebSocket;

// Mensaje base para comunicación WebSocket de batalla
public class WebSocketMessage {
    public required string Type { get; set; }
    public object? Data { get; set; }
}

// Datos del snapshot de batalla que se envía al cliente
public class BattleStateData {
    public required BattleSnapshot Battle { get; set; }
    public List<string> Messages { get; set; } = new();
    public bool RequiresSwitch { get; set; } = false;
    public string? WinnerSide { get; set; } = null; // "Player" | "Opponent" | null
}

// Snapshot completo del estado de la batalla
public class BattleSnapshot {
    public required string BattleId { get; set; }
    public required BattleSideSnapshot PlayerSide { get; set; }
    public required BattleSideSnapshot OpponentSide { get; set; }
    public int Turn { get; set; } = 1;
}

// Estado de un lado de la batalla (player1 o player2)
public class BattleSideSnapshot {
    public required List<PokemonSnapshot> Team { get; set; } // 6 Pokémon
    public int ActiveSlot { get; set; } = 0; // Índice del Pokémon activo (0-5)
}

// Snapshot de un Pokémon en el equipo
public class PokemonSnapshot {
    public int PokemonId { get; set; }
    public required string Name { get; set; }
    public string? Nickname { get; set; }
    public int Slot { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public bool IsFainted { get; set; }
    public required string Status { get; set; } // "None", "Paralyzed", etc.
    public string? SpriteFront { get; set; }
    public string? SpriteBack { get; set; }
    public bool Shiny { get; set; }

    // Solo mostramos estos detalles si el Pokémon está activo
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
    public int Pp { get; set; }
    public required string Type { get; set; }
}