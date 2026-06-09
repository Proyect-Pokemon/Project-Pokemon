namespace ProjectPokemon.Models.Battle.Movements;

// Resultado de la ejecución de un movimiento

public class MovementResult {
    // Indica si el movimiento se ejecutó (true) o falló por falta de PP o precisión (false)
    public bool Executed { get; set; } = false;

    // Indica si el movimiento falló por falta de PP
    public bool FailedByNoPp { get; set; } = false;

    // Indica si el movimiento falló por precisión
    public bool FailedByAccuracy { get; set; } = false;

    // Indica si el movimiento fue un golpe crítico
    public bool IsCritical { get; set; } = false;

    // Multiplicador de efectividad del tipo (0, 0.25, 0.5, 1, 2, 4)
    public double TypeEffectiveness { get; set; } = 1.0;

    // Cantidad de daño infligido
    public int Damage { get; set; } = 0;

    // Cantidad de curación realizada
    public int Healing { get; set; } = 0;

    // Indica si el defensor es inmune al movimiento
    public bool IsImmune { get; set; } = false;

    // Indica si se aplicó el estado Seeded (Drenadoras/Leech Seed)
    public bool AppliedSeeded { get; set; } = false;

    // Indica si el objetivo era inmune a Drenadoras (tipo Planta)
    public bool ImmuneToSeeded { get; set; } = false;

    // Indica si el movimiento falló porque el objetivo ya tiene un estado alterado primario
    public bool FailedByExistingStatus { get; set; } = false;

    // Número de golpes realizados (para movimientos multi-golpe como Doble Bofetón, Púas, etc.)
    public int HitCount { get; set; } = 1;
}
