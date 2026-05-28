using ProjectPokemon.Networking.Messages.Battle;

namespace ProjectPokemon.Services.Battle;

/// <summary>
/// Helper para construir ReplaySteps de forma ordenada durante la resolución de turnos.
/// Mantiene el orden de ejecución y agrupa mensajes con sus eventos asociados.
/// </summary>
public class ReplayStepBuilder {
    private readonly List<ReplayStep> _steps = new();
    private int _nextStepIndex = 0;

    /// <summary>
    /// Crea un nuevo step con un mensaje de texto simple
    /// </summary>
    public ReplayStepBuilder AddMessageStep(string message, int? delayMs = null) {
        var step = new ReplayStep {
            StepIndex = _nextStepIndex++,
            Message = message,
            DelayMs = delayMs
        };
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Crea un nuevo step con un mensaje estructurado
    /// </summary>
    public ReplayStepBuilder AddStructuredStep(
        string? textMessage,
        StructuredBattleMessage? structuredMessage,
        int? delayMs = null) {

        var step = new ReplayStep {
            StepIndex = _nextStepIndex++,
            Message = textMessage,
            StructuredMessage = structuredMessage,
            DelayMs = delayMs
        };
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Crea un nuevo step con mensaje(s) y evento(s) asociados
    /// </summary>
    public ReplayStepBuilder AddStep(
        string? textMessage = null,
        StructuredBattleMessage? structuredMessage = null,
        BattleEvent? singleEvent = null,
        List<BattleEvent>? events = null,
        int? delayMs = null,
        Dictionary<string, object>? metadata = null) {

        var step = new ReplayStep {
            StepIndex = _nextStepIndex++,
            Message = textMessage,
            StructuredMessage = structuredMessage,
            DelayMs = delayMs,
            Metadata = metadata
        };

        if (singleEvent != null) {
            step.Events.Add(singleEvent);
        }

        if (events != null && events.Count > 0) {
            step.Events.AddRange(events);
        }

        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Añade un evento al último step creado
    /// </summary>
    public ReplayStepBuilder AddEventToLastStep(BattleEvent battleEvent) {
        if (_steps.Count > 0) {
            _steps[^1].Events.Add(battleEvent);
        }
        return this;
    }

    /// <summary>
    /// Añade eventos al último step creado
    /// </summary>
    public ReplayStepBuilder AddEventsToLastStep(List<BattleEvent> events) {
        if (_steps.Count > 0 && events.Count > 0) {
            _steps[^1].Events.AddRange(events);
        }
        return this;
    }

    /// <summary>
    /// Añade un mensaje estructurado al último step creado
    /// </summary>
    public ReplayStepBuilder AddStructuredMessageToLastStep(StructuredBattleMessage structuredMessage) {
        if (_steps.Count > 0) {
            _steps[^1].StructuredMessage = structuredMessage;
        }
        return this;
    }

    /// <summary>
    /// Obtiene la lista final de steps construidos
    /// </summary>
    public List<ReplayStep> Build() {
        return _steps;
    }

    /// <summary>
    /// Resetea el builder para reutilización
    /// </summary>
    public void Reset() {
        _steps.Clear();
        _nextStepIndex = 0;
    }

    /// <summary>
    /// Obtiene el número de steps construidos hasta ahora
    /// </summary>
    public int Count => _steps.Count;

    /// <summary>
    /// Obtiene el último step construido (o null si no hay steps)
    /// </summary>
    public ReplayStep? LastStep => _steps.Count > 0 ? _steps[^1] : null;
}
