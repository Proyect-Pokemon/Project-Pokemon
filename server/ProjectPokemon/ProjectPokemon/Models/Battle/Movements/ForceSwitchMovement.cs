using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

/// <summary>
/// Movimientos que fuerzan al oponente a cambiar de Pokémon.
/// El Pokémon objetivo es reemplazado por otro Pokémon aleatorio de su equipo.
/// Si no hay más Pokémon disponibles en el equipo enemigo, el movimiento falla.
/// Ejemplos: Whirlwind (Torbellino), Roar (Rugido), Dragon Tail (Cola Dragón), Circle Throw (Llave Giro).
/// </summary>
public class ForceSwitchMovement : BattleMovement {
    public ForceSwitchMovement(Movement movement) : base(movement) { }

    public override void ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        if (!HasPpAvailable()) {
            return;
        }

        ConsumePp();

        // Comprobar si acierta
        if (!CheckAccuracy(attacker, defender)) {
            return; // El movimiento falla
        }

        // El movimiento no causa daño, solo fuerza el cambio
        // La lógica de cambio se manejará en BattleService ya que necesita acceso al BattleSide
    }
}
