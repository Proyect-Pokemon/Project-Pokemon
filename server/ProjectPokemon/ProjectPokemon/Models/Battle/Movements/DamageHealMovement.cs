using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

/// <summary>
/// Movimiento que causa daño y cura al usuario un porcentaje del daño infligido.
/// Usa la propiedad Drain para determinar el porcentaje de curación (1-100).
/// Ejemplo: Absorb, Mega Drain, Giga Drain, Leech Life, Dream Eater.
/// </summary>
public class DamageHealMovement : DamageMovement {
    private const int DREAM_EATER_ID = 138;

    public DamageHealMovement(Movement movement) : base(movement) { }

    public override MovementResult ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        var result = new MovementResult();

        if (!HasPpAvailable()) {
            result.FailedByNoPp = true;
            return result;
        }

        ConsumePp();

        // Dream Eater (ID 138) requiere que el objetivo esté dormido
        if (Id == DREAM_EATER_ID && defender.Status != PokeStatus.Sleep) {
            // El movimiento falla si el objetivo no está dormido
            result.FailedByAccuracy = true; // Usamos este flag para indicar fallo general
            return result;
        }

        // Comprobar si acierta
        if (!CheckAccuracy(attacker, defender)) {
            result.FailedByAccuracy = true;
            return result;
        }

        result.Executed = true;

        // Calcular daño usando la lógica de DamageMovement con metadata
        int damage = CalculateDamageWithMetadata(attacker, defender, result);

        if (damage > 0) {
            defender.TakeDamage(damage);
            result.Damage = damage;

            // Calcular curación basada en el daño infligido
            int drainPercent = Drain ?? 50; // Por defecto 50% si no está especificado
            int healAmount = Math.Max(1, (damage * drainPercent) / 100);

            // Limitar la curación al HP máximo del atacante
            int actualHealing = Math.Min(healAmount, attacker.MaxHp - attacker.CurrentHp);

            if (actualHealing > 0) {
                attacker.Heal(actualHealing);
            }

            result.Healing = actualHealing;
        }

        return result;
    }
}
