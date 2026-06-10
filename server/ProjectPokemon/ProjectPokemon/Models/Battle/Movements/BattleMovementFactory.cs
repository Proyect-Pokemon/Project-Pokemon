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

        // Si el movimiento tiene categoría "net-good-stats" es un StatChangeMovement
        if (movement.Category == "net-good-stats") {
            return new StatChangeMovement(movement);
        }

        // Si el movimiento tiene categoría "heal" es un HealMovement
        if (movement.Category == "heal") {
            return new HealMovement(movement);
        }

        // Si el movimiento tiene categoría "damage-heal" es un DamageHealMovement
        if (movement.Category == "damage-heal") {
            return new DamageHealMovement(movement);
        }

        // Si el movimiento tiene categoría "damage-lower" es un DamageLowerMovement
        if (movement.Category == "damage-lower") {
            return new DamageLowerMovement(movement);
        }

        // Si el movimiento tiene categoría "damage-ailment" es un DamageAilmentMovement
        if (movement.Category == "damage-ailment") {
            return new DamageAilmentMovement(movement);
        }

        // Si el movimiento tiene categoría "ohko" es un OhkoMovement
        if (movement.Category == "ohko") {
            return new OhkoMovement(movement);
        }

        // Si el movimiento tiene categoría "whole-field-effect" es un WholeFieldEffectMovement
        if (movement.Category == "whole-field-effect") {
            return new WholeFieldEffectMovement(movement);
        }

        // Si el movimiento tiene categoría "force-switch" es un ForceSwitchMovement
        if (movement.Category == "force-switch") {
            return new ForceSwitchMovement(movement);
        }

        // Si el movimiento tiene categoría "field-effect" es un FieldEffectMovement
        if (movement.Category == "field-effect") {
            return new FieldEffectMovement(movement);
        }

        // TODO: Implementar otros tipos de movimientos según Category:
        // - "unique": UniqueMovement (movimientos especiales únicos)
        // Por ahora, devolver DamageMovement como fallback
        return new DamageMovement(movement);
    }
}