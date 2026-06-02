namespace ProjectPokemon.Networking.Messages.Battle;

// Representa un paso atómico en la reproducción de un turno de batalla.
// Cada step contiene mensaje(s), evento(s) asociado(s) y metadata para reproducción secuencial determinista.
public class ReplayStep {
    // Índice del paso en la secuencia del turno (0-based, orden de ejecución)
    public int StepIndex { get; set; }

    // Mensaje de texto legacy para compatibilidad y logs (puede ser null si solo hay eventos estructurados)
    public string? Message { get; set; }

    // Mensaje estructurado con code + args para internacionalización y UI estructurada
    public StructuredBattleMessage? StructuredMessage { get; set; }

    // Lista de eventos de batalla asociados a este paso (AttackEvent, HpChangeEvent, etc.)
    // Todos los eventos en esta lista ocurren como parte de esta acción atómica
    public List<BattleEvent> Events { get; set; } = new();

    // Delay recomendado en milisegundos antes de procesar el siguiente step (para ritmo de animaciones)
    public int? DelayMs { get; set; }

    // Metadata adicional para el frontend (ej: "animation_type": "critical_hit")
    public Dictionary<string, object>? Metadata { get; set; }
}
