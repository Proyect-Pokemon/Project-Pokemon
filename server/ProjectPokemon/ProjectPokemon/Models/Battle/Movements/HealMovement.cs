using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Movimiento que cura al usuario un porcentaje de su vida máxima.
// Típicamente cura el 50% de HP máximo.
// El target siempre es el usuario (self).

public class HealMovement : BattleMovement {
    public HealMovement(Movement movement) : base(movement) { }

    public override MovementResult ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        var result = new MovementResult();

        if (!HasPpAvailable()) {
            result.FailedByNoPp = true;
            return result;
        }

        ConsumePp();

        // Los movimientos de curación no requieren accuracy check
        // Siempre se ejecutan correctamente sobre el usuario

        result.Executed = true;

        // Healing es el porcentaje de HP máximo a recuperar (típicamente 50)
        int healingPercent = Healing ?? 50; // Por defecto 50% si no está especificado
        int healAmount = Math.Max(1, (attacker.MaxHp * healingPercent) / 100);

        // Limitar la curación al HP máximo
        int actualHealing = Math.Min(healAmount, attacker.MaxHp - attacker.CurrentHp);

        if (actualHealing > 0) {
            attacker.Heal(actualHealing);
        }

        result.Healing = actualHealing;
        return result;
    }
}
