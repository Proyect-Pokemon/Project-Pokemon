using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Factory para crear la instancia correcta de BattleMovement según la categoría del movimiento.
public static class BattleMovementFactory {
    public static BattleMovement Create(Movement movement) {
        // Si el movimiento tiene categoría "ailment" es un AilmentMovement
        if (movement.Category == "ailment") {
            return new AilmentMovement(movement);
        }

        // Si el movimiento tiene categoría "damage" es un DamageMovement
        if (movement.Category == "damage") {
            return new DamageMovement(movement);
        }

        // TODO: Implementar otros tipos de movimientos según Category:
        // - "damage-ailment": DamageAilmentMovement (movimiento que hace daño Y aplica estado)
        // - "damage-heal": DamageHealMovement (movimiento que hace daño Y cura al atacante)
        // - "damage-lower": DamageLowerMovement (movimiento que hace daño Y baja stats del defensor)
        // - "field-effect": FieldEffectMovement (efectos de campo en un lado)
        // - "force-switch": ForceSwitchMovement (movimientos que fuerzan cambio de pokémon)
        // - "heal": HealMovement (movimientos que solo curan)
        // - "net-good-stats": StatChangeMovement (movimientos que cambian stats)
        // - "ohko": OhkoMovement (movimientos fulminantes)
        // - "unique": UniqueMovement (movimientos especiales únicos)
        // - "whole-field-effect": WholeFieldEffectMovement (efectos de campo globales)
        // Por ahora, devolver DamageMovement como fallback
        return new DamageMovement(movement);
    }
}
