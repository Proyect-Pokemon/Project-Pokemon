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

    public override void ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        if (!HasPpAvailable()) {
            return;
        }

        ConsumePp();

        // Dream Eater (ID 138) requiere que el objetivo esté dormido
        if (Id == DREAM_EATER_ID && defender.Status != PokeStatus.Sleep) {
            // El movimiento falla si el objetivo no está dormido
            return;
        }

        // Comprobar si acierta
        if (!CheckAccuracy(attacker, defender)) {
            return; // El movimiento falla
        }

        // Calcular daño usando la lógica de DamageMovement
        int damage = CalculateDamage(attacker, defender);

        if (damage > 0) {
            defender.TakeDamage(damage);

            // Calcular curación basada en el daño infligido
            int drainPercent = Drain ?? 50; // Por defecto 50% si no está especificado
            int healAmount = Math.Max(1, (damage * drainPercent) / 100);

            // Limitar la curación al HP máximo del atacante
            int actualHealing = Math.Min(healAmount, attacker.MaxHp - attacker.CurrentHp);

            if (actualHealing > 0) {
                attacker.Heal(actualHealing);
            }
        }
    }
}
