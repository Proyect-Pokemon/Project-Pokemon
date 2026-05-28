namespace ProjectPokemon.Networking.Messages.Battle;

/// <summary>
/// Representa un paso atómico en la reproducción de un turno de batalla.
/// Cada step contiene mensaje(s), evento(s) asociado(s) y metadata para reproducción secuencial determinista.
/// </summary>
public class ReplayStep {
    /// <summary>
    /// Índice del paso en la secuencia del turno (0-based, orden de ejecución)
    /// </summary>
    public int StepIndex { get; set; }

    /// <summary>
    /// Mensaje de texto legacy para compatibilidad y logs (puede ser null si solo hay eventos estructurados)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Mensaje estructurado con code + args para internacionalización y UI estructurada
    /// </summary>
    public StructuredBattleMessage? StructuredMessage { get; set; }

    /// <summary>
    /// Lista de eventos de batalla asociados a este paso (AttackEvent, HpChangeEvent, etc.)
    /// Todos los eventos en esta lista ocurren como parte de esta acción atómica
    /// </summary>
    public List<BattleEvent> Events { get; set; } = new();

    /// <summary>
    /// Delay recomendado en milisegundos antes de procesar el siguiente step (para ritmo de animaciones)
    /// Null = usar delay por defecto del cliente
    /// </summary>
    public int? DelayMs { get; set; }

    /// <summary>
    /// Metadata adicional para el frontend (ej: "animation_type": "critical_hit")
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
