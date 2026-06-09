using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities;

/// <summary>
/// Representa un cambio de estadística asociado a un movimiento.
/// Un movimiento puede tener múltiples cambios de estadísticas.
/// </summary>
public class MovementStatChange {
    public int Id { get; set; }
    public int MovementId { get; set; }
    public required StatType Stat { get; set; }  // La estadística que se modifica
    public required int Change { get; set; }     // La cantidad de cambio (positivo o negativo)

    // Relación con Movement
    public Movement Movement { get; set; } = null!;
}