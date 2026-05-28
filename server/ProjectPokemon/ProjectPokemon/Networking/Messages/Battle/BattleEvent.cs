using System.Text.Json.Serialization;

namespace ProjectPokemon.Networking.Messages.Battle;

// Base para todos los eventos de batalla
// Configuración de serialización polimórfica para System.Text.Json
[JsonDerivedType(typeof(MessageEvent), typeDiscriminator: "message")]
[JsonDerivedType(typeof(AttackEvent), typeDiscriminator: "attack")]
[JsonDerivedType(typeof(HpChangeEvent), typeDiscriminator: "hp_change")]
[JsonDerivedType(typeof(StatusChangeEvent), typeDiscriminator: "status_change")]
[JsonDerivedType(typeof(SecondaryStatusChangeEvent), typeDiscriminator: "secondary_status_change")]
[JsonDerivedType(typeof(FaintEvent), typeDiscriminator: "faint")]
[JsonDerivedType(typeof(SwitchEvent), typeDiscriminator: "switch")]
[JsonDerivedType(typeof(StatStageChangeEvent), typeDiscriminator: "stat_stage_change")]
[JsonDerivedType(typeof(BattleEndEvent), typeDiscriminator: "battle_end")]
public abstract class BattleEvent {
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty; // Texto legacy para compatibilidad
}

// Identificador de un Pokémon en batalla
public class PokemonIdentifier {
    public required string Side { get; set; } // "player" o "opponent"
    public int Slot { get; set; }
    public required string DisplayName { get; set; }
}

// Evento: mensaje genérico (para anuncios sin cambio de estado)
public class MessageEvent : BattleEvent {
    public MessageEvent() {
        EventType = "message";
        Message = "";
    }
}

// Evento: intento de ataque
public class AttackEvent : BattleEvent {
    public AttackEvent() {
        EventType = "attack";
        Message = "";
    }

    public required PokemonIdentifier Attacker { get; set; }
    public required PokemonIdentifier Defender { get; set; }
    public required string MoveName { get; set; }
    public bool Hit { get; set; } = true;
    public bool Blocked { get; set; } = false; // No pudo atacar (freeze, sleep, etc.)
    public string? BlockReason { get; set; } // "frozen", "asleep", "paralyzed", "confused"
}

// Evento: cambio de HP
public class HpChangeEvent : BattleEvent {
    public HpChangeEvent() {
        EventType = "hp_change";
        Message = "";
    }

    public required PokemonIdentifier Target { get; set; }
    public int BeforeHp { get; set; }
    public int AfterHp { get; set; }
    public int MaxHp { get; set; }
    public int Amount { get; set; } // Cantidad de daño/curación (positivo = curación, negativo = daño)
    public required string Cause { get; set; } // "move", "burn", "poison", "badly_poisoned", "leech_seed", "confusion_self_hit"
    public string? SourceMove { get; set; } // Nombre del movimiento si Cause = "move"
    public PokemonIdentifier? SourcePokemon { get; set; } // Quien causó el cambio (para leech_seed)
}

// Evento: cambio de estado primario
public class StatusChangeEvent : BattleEvent {
    public StatusChangeEvent() {
        EventType = "status_change";
        Message = "";
    }

    public required PokemonIdentifier Target { get; set; }
    public required string BeforeStatus { get; set; }
    public required string AfterStatus { get; set; }
    public string? Cause { get; set; } // "move", "thawed", "woke_up"
}

// Evento: cambio de estados secundarios
public class SecondaryStatusChangeEvent : BattleEvent {
    public SecondaryStatusChangeEvent() {
        EventType = "secondary_status_change";
        Message = "";
    }

    public required PokemonIdentifier Target { get; set; }
    public required string SecondaryStatus { get; set; }
    public bool Added { get; set; } // true = añadido, false = eliminado
}

// Evento: Pokémon debilitado
public class FaintEvent : BattleEvent {
    public FaintEvent() {
        EventType = "faint";
        Message = "";
    }

    public required PokemonIdentifier Target { get; set; }
}

// Evento: cambio de Pokémon activo
public class SwitchEvent : BattleEvent {
    public SwitchEvent() {
        EventType = "switch";
        Message = "";
    }

    public required string Side { get; set; }
    public int PreviousActiveSlot { get; set; }
    public int NewActiveSlot { get; set; }
    public required string NewPokemonName { get; set; }
    public bool IsAutomatic { get; set; } // true si es por KO, false si es acción del jugador
}

// Evento: cambio de estadísticas (stages)
public class StatStageChangeEvent : BattleEvent {
    public StatStageChangeEvent() {
        EventType = "stat_stage_change";
        Message = "";
    }

    public required PokemonIdentifier Target { get; set; }
    public required string Stat { get; set; } // "Attack", "Defense", etc.
    public int Change { get; set; } // +1, -2, etc.
    public int NewStage { get; set; } // Valor absoluto del stage después del cambio
}

// Evento: fin de batalla
public class BattleEndEvent : BattleEvent {
    public BattleEndEvent() {
        EventType = "battle_end";
        Message = "";
    }

    public required string Winner { get; set; } // "player" o "opponent"
    public int? WinnerUserId { get; set; }
}
