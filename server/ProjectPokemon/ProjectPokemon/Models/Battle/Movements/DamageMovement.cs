using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Battle.Movements;

// Movimientos de categoría "Damage".
// Inflingen daño directo al oponente.

public class DamageMovement : BattleMovement {

    public DamageMovement(Movement movement) : base(movement) { }

    // Realizar el movimiento. Devuelve el daño realizado al defensor
    public override void ExecuteMovement(PokemonBattle attacker, PokemonBattle defender) {
        if (!HasPpAvailable()) {
            return; // No quedan PPs
        }

        ConsumePp();

        // Comprobar si acierta
        if (!CheckAccuracy(attacker, defender)) {
            return; // El movimiento falla
        }

        int damage = CalculateDamage(attacker, defender);
        defender.TakeDamage(damage);
    }

    public virtual int CalculateDamage(PokemonBattle attacker, PokemonBattle defender) {
        // Averiguar si el movimiento es físico o especial, y usar el stat que corresponda con stages aplicados
        StatType attackStatType = MovementClass == MovementClass.Physical ? StatType.Attack : StatType.SpecialAttack;
        StatType defenseStatType = MovementClass == MovementClass.Physical ? StatType.Defense : StatType.SpecialDefense;

        int attackStat = attacker.GetModifiedStat(attackStatType);
        int defenseStat = defender.GetModifiedStat(defenseStatType);

        // Calcular el daño base
        int level = 50;

        double baseDamage = ((2.0 * level / 5.0) + 2) * Power!.Value * attackStat / defenseStat;
        baseDamage = (baseDamage / 50.0) + 2;

        double damage = baseDamage;
        damage *= GetStabModifier(attacker);
        damage *= GetEffectiveness(defender);
        damage *= GetRandomModifier();
        damage *= GetCriticalModifier(attacker);

        return (int)Math.Floor(damage);
    }

    // Comprobar si hay STAB
    protected virtual double GetStabModifier(PokemonBattle attacker) {
        if (attacker.Type1 == Type || attacker.Type2 == Type) {
            return 1.5;
        }
        return 1.0;
    }

    protected virtual double GetEffectiveness(PokemonBattle defender) {
        return TypeEffectivenessChart.GetTypeEffectiveness(Type, defender.Type1, defender.Type2);
    }

    // Calcular el modificador aleatorio de máx y mín damage
    protected virtual double GetRandomModifier() {
        Random random = new Random();
        return random.NextDouble() * 0.15 + 0.85; // Entre 0.85 y 1.0
    }

    // Calcular si es o no es crítico
    protected virtual double GetCriticalModifier(PokemonBattle attacker) {
        int critStage = CritRate;
        double critChance = critStage switch {
            0 => 1.0 / 16,  // 6.25%
            1 => 1.0 / 8,   // 12.5%
            2 => 1.0 / 4,   // 25%
            3 => 1.0 / 3,   // 33.33%
            4 => 1.0 / 2,   // 50%
            _ => 1.0        // Siempre es crítico
        };

        Random random = new Random();
        if (random.NextDouble() < critChance) {
            return 1.5; // Multiplicador de daño crítico
        }
        return 1.0; // No es crítico
    }
}
