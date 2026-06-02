using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Movimientos OHKO (One-Hit Knockout) que debilitan al oponente en un solo golpe si aciertan.
// Ejemplos: Guillotina (Guillotine), Fisura (Fissure), Frío Polar (Sheer Cold), Cuerno Taladro (Horn Drill).

// NOTA: En los juegos Pokémon oficiales, estos movimientos:
// - Fallan automáticamente si el usuario tiene menor nivel que el objetivo
// - Tienen accuracy base + (nivel usuario - nivel objetivo) si el usuario tiene mayor o igual nivel

// En nuestra aplicación, todos los Pokémon tienen nivel fijo (50), por lo que simplemente
// usamos el Accuracy definido en la base de datos (típicamente 30%).
public class OhkoMovement : BattleMovement {
    public OhkoMovement(Movement movement) : base(movement) { }

    public override MovementResult ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        var result = new MovementResult();

        if (!HasPpAvailable()) {
            result.FailedByNoPp = true;
            return result;
        }

        ConsumePp();

        // Comprobar accuracy
        // NOTA: En la mecánica oficial, si el usuario tiene menor nivel que el defensor,
        // el movimiento falla automáticamente. Como todos nuestros Pokémon tienen nivel 50,
        // simplemente usamos el accuracy normal del movimiento.
        if (!CheckAccuracy(attacker, defender)) {
            result.FailedByAccuracy = true;
            return result;
        }

        result.Executed = true;

        // Si acierta, debilitar completamente al defensor (OHKO)
        int damage = defender.CurrentHp; // Daño = HP actual del defensor
        defender.TakeDamage(damage);
        result.Damage = damage;

        return result;
    }
}
