using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Movimiento que causa daño y tiene probabilidad de aplicar un estado alterado.
// Usa AilmentChance para determinar la probabilidad (1-100) de aplicar el estado.
// El estado se define en Ailment.
// Casos especiales:
// - Trap moves (Bind, Wrap, Fire Spin, Clamp): usan MinTurns/MaxTurns para duración del atrapamiento
// - Tri-Attack (ID 161): puede aplicar burn, freeze o paralysis aleatoriamente

public class DamageAilmentMovement : DamageMovement {
    private const int TRI_ATTACK_ID = 161;

    // IDs de movimientos trap que atrapan al oponente
    private static readonly HashSet<int> TRAP_MOVE_IDS = new() { 20, 35, 83, 128 };

    public DamageAilmentMovement(Movement movement) : base(movement) { }

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

        // Determinar el número de golpes si es un movimiento multi-golpe
        int hitCount = 1;
        if (MinHits.HasValue && MaxHits.HasValue) {
            Random random = new Random();
            hitCount = random.Next(MinHits.Value, MaxHits.Value + 1);
        }
        result.HitCount = hitCount;

        // Ejecutar cada golpe
        int totalDamage = 0;
        for (int i = 0; i < hitCount; i++) {
            int damage = CalculateDamageWithMetadata(attacker, defender, result);
            defender.TakeDamage(damage);
            totalDamage += damage;

            // Si el defensor se debilita, detener los golpes
            if (defender.IsFainted()) {
                result.HitCount = i + 1; // Actualizar al número real de golpes
                break;
            }
        }

        result.Damage = totalDamage;

        // Después del daño, aplicar efecto secundario de ailment
        // si el defensor sigue vivo y se cumple la probabilidad
        if (!defender.IsFainted() && AilmentChance > 0) {
            Random random = new Random();
            int roll = random.Next(1, 101);

            if (roll <= AilmentChance) {
                ApplyAilment(attacker, defender, random);
            }
        }

        return result;
    }

    private void ApplyAilment(PokemonBattle attacker, PokemonBattle defender, Random random) {
        // Caso especial: Tri-Attack (puede aplicar burn, freeze o paralysis)
        if (Id == TRI_ATTACK_ID) {
            ApplyRandomStatusFromTriAttack(defender, random);
            return;
        }

        // Caso especial: Movimientos trap (Bind, Wrap, Fire Spin, Clamp)
        if (TRAP_MOVE_IDS.Contains(Id)) {
            ApplyTrapEffect(attacker, defender, random);
            return;
        }

        // Aplicar estado alterado normal
        ApplyStandardAilment(defender);
    }

    private void ApplyRandomStatusFromTriAttack(PokemonBattle defender, Random random) {
        // Elegir aleatoriamente entre burn (0), freeze (1) o paralysis (2)
        int statusChoice = random.Next(0, 3);

        PokeStatus statusToApply = statusChoice switch {
            0 => PokeStatus.Burn,
            1 => PokeStatus.Freeze,
            2 => PokeStatus.Paralysis,
            _ => PokeStatus.None
        };

        // Verificar inmunidades de tipo antes de aplicar
        if (CanApplyStatus(defender, statusToApply)) {
            defender.Status = statusToApply;
        }
    }

    private void ApplyTrapEffect(PokemonBattle attacker, PokemonBattle defender, Random random) {
        // Los movimientos trap aplican el estado secundario "Bound"
        // y establecen la duración según MinTurns y MaxTurns
        if (!defender.HasSecondaryStatus(PokeSecondaryStatus.Bound)) {
            defender.AddSecondaryStatus(PokeSecondaryStatus.Bound);

            // Determinar duración del trap
            int duration = MinTurns ?? 0;
            if (MaxTurns.HasValue && MaxTurns > duration) {
                duration = random.Next(duration, MaxTurns.Value + 1);
            }

            defender.BoundTurnsRemaining = duration;
            defender.BoundSource = attacker; // Guardar referencia al atacante
        }
    }

    private void ApplyStandardAilment(PokemonBattle defender) {
        // Mapear el string del ailment al enum PokeStatus
        PokeStatus statusToApply = Ailment?.ToLower() switch {
            "burn" => PokeStatus.Burn,
            "freeze" => PokeStatus.Freeze,
            "paralysis" => PokeStatus.Paralysis,
            "poison" => PokeStatus.Poison,
            "sleep" => PokeStatus.Sleep,
            "badly-poison" => PokeStatus.BadlyPoisoned,
            // Estados secundarios
            "confusion" => PokeStatus.None, // Se maneja aparte
            _ => PokeStatus.None
        };

        // Si es confusión, aplicar estado secundario
        if (Ailment?.ToLower() == "confusion") {
            if (!defender.HasSecondaryStatus(PokeSecondaryStatus.Confuse)) {
                defender.AddSecondaryStatus(PokeSecondaryStatus.Confuse);
                Random random = new Random();
                defender.ConfusionTurnsRemaining = random.Next(2, 6); // 2-5 turnos
            }
            return;
        }

        // Aplicar estado primario si es válido y el defensor puede recibirlo
        if (statusToApply != PokeStatus.None && CanApplyStatus(defender, statusToApply)) {
            defender.Status = statusToApply;

            // Configurar turnos de sueño si es necesario
            if (statusToApply == PokeStatus.Sleep) {
                Random random = new Random();
                defender.SleepTurnsRemaining = random.Next(1, 4); // 1-3 turnos
            }
        }
    }

    private bool CanApplyStatus(PokemonBattle defender, PokeStatus status) {
        // No se puede aplicar estado si ya tiene uno
        if (defender.Status != PokeStatus.None) {
            return false;
        }

        // Verificar inmunidades de tipo
        return status switch {
            PokeStatus.Burn => defender.Type1 != PokeType.Fire && defender.Type2 != PokeType.Fire,
            PokeStatus.Freeze => defender.Type1 != PokeType.Ice && defender.Type2 != PokeType.Ice,
            PokeStatus.Paralysis => defender.Type1 != PokeType.Electric && defender.Type2 != PokeType.Electric,
            PokeStatus.Poison or PokeStatus.BadlyPoisoned => 
                defender.Type1 != PokeType.Poison && defender.Type2 != PokeType.Poison &&
                defender.Type1 != PokeType.Steel && defender.Type2 != PokeType.Steel,
            _ => true
        };
    }
}
