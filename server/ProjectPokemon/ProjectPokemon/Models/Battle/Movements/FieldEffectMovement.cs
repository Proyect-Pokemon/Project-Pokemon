using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

/// <summary>
/// Movimientos que crean efectos de campo en un lado de la batalla.
/// Estos efectos duran varios turnos y afectan al equipo que los usa.
/// 
/// Movimientos implementados:
/// - Mist/Niebla (ID 54): Protege las estadísticas del usuario de ser reducidas por el enemigo durante 5 turnos.
/// - Light Screen/Pantalla de Luz (ID 113): Reduce el daño de ataques especiales enemigos a la mitad durante 5 turnos.
/// - Reflect/Reflejo (ID 115): Reduce el daño de ataques físicos enemigos a la mitad durante 5 turnos.
/// </summary>
public class FieldEffectMovement : BattleMovement {
    private const int MIST_ID = 54;
    private const int LIGHT_SCREEN_ID = 113;
    private const int REFLECT_ID = 115;

    public FieldEffectMovement(Movement movement) : base(movement) { }

    public override MovementResult ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        var result = new MovementResult();

        if (!HasPpAvailable()) {
            result.FailedByNoPp = true;
            return result;
        }

        ConsumePp();

        result.Executed = true;

        // Los efectos de campo no requieren accuracy check
        // Se aplican directamente al lado del usuario

        // La lógica específica de cada efecto se maneja en BattleService
        // ya que necesita acceso al BattleSide para establecer los contadores de turnos

        // Aquí solo validamos que el movimiento se ejecute correctamente

        return result;
    }
}
