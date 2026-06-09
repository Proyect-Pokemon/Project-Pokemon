using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Categoría "net-good-stats"
// Movimientos que solo modifican estadísticas (suben las del usuario o bajan las del oponente)
public class StatChangeMovement : BattleMovement {

    public StatChangeMovement(Movement movement) : base(movement) { }

    public override MovementResult ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        var result = new MovementResult();

        if (!HasPpAvailable()) {
            result.FailedByNoPp = true;
            return result;
        }

        ConsumePp();

        if (!CheckAccuracy(attacker, defender)) {
            result.FailedByAccuracy = true;
            return result;
        }

        result.Executed = true;

        // Verificar si hay cambios de estadísticas definidos
        if (StatChanges == null || StatChanges.Count == 0) {
            return result;
        }

        // Determinar el objetivo según el Target del movimiento
        PokemonBattle target = Target == PokeTarget.User ? attacker : defender;

        // Aplicar todos los cambios de estadísticas
        foreach (var statChange in StatChanges) {
            target.ModifyStage(statChange.Stat, statChange.Change);
        }

        return result;
    }
}