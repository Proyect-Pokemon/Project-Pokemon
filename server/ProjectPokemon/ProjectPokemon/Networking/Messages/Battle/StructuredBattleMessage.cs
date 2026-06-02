using System.Text.Json.Serialization;

namespace ProjectPokemon.Networking.Messages.Battle;

// Mensaje estructurado de combate para el frontend
// Reemplaza las narrativas largas con codigos + args
public class StructuredBattleMessage {
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("args")]
    public Dictionary<string, object> Args { get; set; } = new();
}

// Helper para construir mensajes estructurados
public static class BattleMessageBuilder {
    public static StructuredBattleMessage Create(string code, Dictionary<string, object>? args = null) {
        return new StructuredBattleMessage {
            Code = code,
            Args = args ?? new Dictionary<string, object>()
        };
    }

    // Helpers para crear identificadores de Pokemon sin tildes
    public static Dictionary<string, object> CreateActorArgs(string actor, string owner) {
        return new Dictionary<string, object> {
            { "actor", actor },
            { "owner", owner }
        };
    }

    public static Dictionary<string, object> CreateActorTargetArgs(
        string actor, string actorOwner, 
        string target, string targetOwner) {
        return new Dictionary<string, object> {
            { "actor", actor },
            { "actor_owner", actorOwner },
            { "target", target },
            { "target_owner", targetOwner }
        };
    }
}
