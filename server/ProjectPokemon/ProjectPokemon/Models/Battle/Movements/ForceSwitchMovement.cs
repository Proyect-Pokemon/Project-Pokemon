using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Movimientos que fuerzan al oponente a cambiar de Pokémon.
// El Pokémon objetivo es reemplazado por otro Pokémon aleatorio de su equipo.
// Si no hay más Pokémon disponibles en el equipo enemigo, el movimiento falla.
// Ejemplos: Whirlwind (Torbellino), Roar (Rugido), Dragon Tail (Cola Dragón), Circle Throw (Llave Giro).
public class ForceSwitchMovement : BattleMovement {
    public ForceSwitchMovement(Movement movement) : base(movement) { }

    public override MovementResult ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        var result = new MovementResult();

        if (!HasPpAvailable()) {
            result.FailedByNoPp = true;
            return result;
        }

        ConsumePp();

        // Comprobar si acierta
        if (!CheckAccuracy(attacker, defender)) {
            result.FailedByAccuracy = true;
            return result;
        }

        result.Executed = true;

        // El movimiento no causa daño, solo fuerza el cambio
        // La lógica de cambio se manejará en BattleService ya que necesita acceso al BattleSide

        return result;
    }
}
