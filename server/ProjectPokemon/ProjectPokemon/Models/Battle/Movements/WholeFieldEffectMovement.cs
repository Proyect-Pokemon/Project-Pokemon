using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

/// <summary>
/// Movimientos que afectan a todo el campo de batalla (ambos lados).
/// Actualmente solo hay un movimiento en esta categoría:
/// - Haze/Niebla (ID 114): Resetea todos los cambios de estadísticas (stages) de todos los Pokémon en combate.
/// </summary>
public class WholeFieldEffectMovement : BattleMovement {
    private const int HAZE_ID = 114;

    public WholeFieldEffectMovement(Movement movement) : base(movement) { }

    public override MovementResult ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        var result = new MovementResult();

        if (!HasPpAvailable()) {
            result.FailedByNoPp = true;
            return result;
        }

        ConsumePp();

        result.Executed = true;

        // Haze (ID 114): Resetea todos los stages de ambos Pokémon en combate
        if (Id == HAZE_ID) {
            // Resetear stages del atacante (usuario)
            attacker.ResetStages();

            // Resetear stages del defensor (oponente)
            defender.ResetStages();
        }

        // Nota: Si en el futuro se añaden más movimientos a esta categoría,
        // se pueden agregar más casos especiales aquí

        return result;
    }
}
