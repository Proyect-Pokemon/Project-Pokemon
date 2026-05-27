using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Categoría "Ailment"
// Movimientos que solo aplican estado alterado (primario o secundario)
public class AilmentMovement : BattleMovement {

    public AilmentMovement(Movement movement) : base(movement) { }

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
        ApplyAilment(attacker, defender, result);
        return result;
    }


    private void ApplyAilment(PokemonBattle attacker, PokemonBattle defender, MovementResult result) {
        // Comprueba si es un estado alterado primario
        var primaryStatus = ConvertToPrimaryStatus(Ailment);
        if (primaryStatus != PokeStatus.None) {
            // Sólo se aplica si el defensor no tiene ya uno
            if (defender.Status == PokeStatus.None) {
                defender.Status = primaryStatus;

                // Si es Sleep, generar turnos aleatorios de sueño (1-4)
                if (primaryStatus == PokeStatus.Sleep) {
                    Random random = new Random();
                    defender.SleepTurnsRemaining = random.Next(1, 5); // 1 a 4 turnos
                }

                // Si es BadlyPoisoned, inicializar el contador en 1
                if (primaryStatus == PokeStatus.BadlyPoisoned) {
                    defender.BadlyPoisonedCounter = 1;
                }
            } else {
                // El objetivo ya tiene un estado alterado primario
                result.FailedByExistingStatus = true;
            }
            return;
        }

        // Si no es un estado alterado primario, será secundario
        var secondaryStatus = ConvertToSecondaryStatus(Ailment);
        if (secondaryStatus != null) {
            // Verificar inmunidad para Leech Seed (tipo Planta es inmune)
            if (secondaryStatus == PokeSecondaryStatus.Seeded) {
                if (defender.Type1 == PokeType.Grass || defender.Type2 == PokeType.Grass) {
                    result.ImmuneToSeeded = true;
                    return; // Pokémon tipo Planta es inmune
                }
                // Marcar que se aplicó Seeded
                // La curación se aplicará al Pokémon activo del lado contrario, no a uno específico
                result.AppliedSeeded = true;
            }

            defender.AddSecondaryStatus(secondaryStatus.Value);

            // Si es Confuse, generar turnos aleatorios de confusión (2-5)
            // Se aumenta el rango para que no se cure en el mismo turno en que se aplica
            if (secondaryStatus == PokeSecondaryStatus.Confuse) {
                Random random = new Random();
                defender.ConfusionTurnsRemaining = random.Next(2, 6); // 2 a 5 turnos
            }
        }
    }

    // Convierte el ailment a un estado principal si corresponde.
    private PokeStatus ConvertToPrimaryStatus(string ailment) {
        return ailment switch {
            "burn" => PokeStatus.Burn,
            "freeze" => PokeStatus.Freeze,
            "paralysis" => PokeStatus.Paralysis,
            "poison" => PokeStatus.Poison,
            "sleep" => PokeStatus.Sleep,
            _ => PokeStatus.None
        };
    }

    // Convierte el ailment a un estado secundario si corresponde.
    private PokeSecondaryStatus? ConvertToSecondaryStatus(string ailment) {
        return ailment switch {
            "confusion" => PokeSecondaryStatus.Confuse,
            "leech-seed" => PokeSecondaryStatus.Seeded,
            "none" => null,
            "unknown" => null, // Triataque tiene ailment unknown
            _ => null
        };
    }
}