using ProjectPokemon.Networking.Messages.Battle;

namespace ProjectPokemon.Services.Battle;

// Helper para construir ReplaySteps de forma ordenada durante la resolución de turnos.
// Mantiene el orden de ejecución y agrupa mensajes con sus eventos asociados.
public class ReplayStepBuilder {
    private readonly List<ReplayStep> _steps = new();
    private int _nextStepIndex = 0;

    // Crea un nuevo step con un mensaje de texto simple
    public ReplayStepBuilder AddMessageStep(string message, int? delayMs = null) {
        var step = new ReplayStep {
            StepIndex = _nextStepIndex++,
            Message = message,
            DelayMs = delayMs
        };
        _steps.Add(step);
        return this;
    }

    // Crea un nuevo step con un mensaje estructurado
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

    // Crea un nuevo step con mensaje(s) y evento(s) asociados
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

    // Añade un evento al último step creado
    public ReplayStepBuilder AddEventToLastStep(BattleEvent battleEvent) {
        if (_steps.Count > 0) {
            _steps[^1].Events.Add(battleEvent);
        }
        return this;
    }

    // Añade eventos al último step creado
    public ReplayStepBuilder AddEventsToLastStep(List<BattleEvent> events) {
        if (_steps.Count > 0 && events.Count > 0) {
            _steps[^1].Events.AddRange(events);
        }
        return this;
    }

    // Añade un mensaje estructurado al último step creado
    public ReplayStepBuilder AddStructuredMessageToLastStep(StructuredBattleMessage structuredMessage) {
        if (_steps.Count > 0) {
            _steps[^1].StructuredMessage = structuredMessage;
        }
        return this;
    }

    // Obtiene la lista final de steps construidos
    public List<ReplayStep> Build() {
        return _steps;
    }

    // Resetea el builder para reutilización
    public void Reset() {
        _steps.Clear();
        _nextStepIndex = 0;
    }

    // Obtiene el número de steps construidos hasta ahora
    public int Count => _steps.Count;

    // Obtiene el último step construido (o null si no hay steps)
    public ReplayStep? LastStep => _steps.Count > 0 ? _steps[^1] : null;

    // Obtiene el primer step construido (o null si no hay steps)
    public ReplayStep? FirstStep => _steps.Count > 0 ? _steps[0] : null;

    // Obtiene un step por índice (o null si está fuera de rango)
    public ReplayStep? GetStep(int index) => index >= 0 && index < _steps.Count ? _steps[index] : null;
}
