using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

/// <summary>
/// Movimiento que causa daño y tiene probabilidad de reducir estadísticas del objetivo.
/// Usa StatChance para determinar la probabilidad (1-100) de que se apliquen los cambios de estadísticas.
/// Los cambios de estadísticas se definen en StatChanges (típicamente cambios negativos al objetivo).
/// Ejemplo: Psychic, Shadow Ball, Crunch, Iron Tail.
/// </summary>
public class DamageLowerMovement : DamageMovement {
    public DamageLowerMovement(Movement movement) : base(movement) { }

    public override void ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        if (!HasPpAvailable()) {
            return;
        }

        ConsumePp();

        // Comprobar si acierta
        if (!CheckAccuracy(attacker, defender)) {
            return; // El movimiento falla
        }

        // Calcular y aplicar daño usando la lógica de DamageMovement
        int damage = CalculateDamage(attacker, defender);
        defender.TakeDamage(damage);

        // Después del daño, aplicar efecto secundario de reducción de estadísticas
        // si el defensor sigue vivo y se cumple la probabilidad
        if (!defender.IsFainted() && StatChanges != null && StatChanges.Count > 0) {
            // StatChance es un porcentaje (típicamente 10)
            Random random = new Random();
            int roll = random.Next(1, 101);

            if (roll <= StatChance) {
                // Aplicar todos los cambios de estadísticas al objetivo
                foreach (var statChange in StatChanges) {
                    defender.ModifyStage(statChange.Stat, statChange.Change);
                }
            }
        }
    }
}
