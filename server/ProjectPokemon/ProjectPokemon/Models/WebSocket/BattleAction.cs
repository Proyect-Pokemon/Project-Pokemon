namespace ProjectPokemon.Models.WebSocket;

// Acción de combate enviada por el cliente
public class BattleActionMessage {
    public required string BattleId { get; set; }
    public required string Action { get; set; } // "attack" | "switch"
    public string? MoveName { get; set; } // Para acción "attack"
    public int? NewActiveSlot { get; set; } // Para acción "switch" (0-5)
}