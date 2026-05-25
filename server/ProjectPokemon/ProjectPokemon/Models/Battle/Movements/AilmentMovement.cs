using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Categoría "Ailment"
// Movimientos que solo aplican estado alterado (primario o secundario)
public class AilmentMovement : BattleMovement {

    public AilmentMovement(Movement movement) : base(movement) { }

    public override void ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        if (!HasPpAvailable()) {
            return;
        }

        ConsumePp();

        if (!CheckAccuracy(attacker, defender)) {
            return;
        }

        // Aplicar el estado alterado si el defensor no tiene ya uno
        // if (TryApplyAilment()) {
        ApplyAilment(attacker, defender);
        // }
    }

    // private bool TryApplyAilment() {
    //    if (AilmentChance == 0) {
    //        return true; // Si no tiene probabilidad, tiene un 100% de probabilidad
    //    }
    //
    //    Random random = new Random();
    //    return random.Next(0, 100) < AilmentChance;
    // }

    private void ApplyAilment(PokemonBattle attacker, PokemonBattle defender) {
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
            }
            return;
        }

        // Si no es un estado alterado primario, será secundario
        var secondaryStatus = ConvertToSecondaryStatus(Ailment);
        if (secondaryStatus != null) {
            // Verificar inmunidad para Leech Seed (tipo Planta es inmune)
            if (secondaryStatus == PokeSecondaryStatus.Seeded) {
                if (defender.Type1 == PokeType.Grass || defender.Type2 == PokeType.Grass) {
                    return; // Pokémon tipo Planta es inmune
                }
                // Guardar referencia al atacante para recuperar PS más tarde
                defender.LeechSeedSource = attacker;
            }

            defender.AddSecondaryStatus(secondaryStatus.Value);

            // Si es Confuse, generar turnos aleatorios de confusión (1-4)
            if (secondaryStatus == PokeSecondaryStatus.Confuse) {
                Random random = new Random();
                defender.ConfusionTurnsRemaining = random.Next(1, 5); // 1 a 4 turnos
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