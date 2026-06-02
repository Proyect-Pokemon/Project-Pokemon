namespace ProjectPokemon.Networking.Messages.Battle;

// Codigos estructurados para mensajes de combate (sin tildes, snake_case)
public static class BattleMessageCode {
    // Acciones de ataque
    public const string AttackUsed = "attack_used";
    public const string AttackMissed = "attack_missed";
    public const string AvoidedAttack = "avoided_attack";
    public const string CriticalHit = "critical_hit";
    public const string NoEffect = "no_effect";
    public const string NotVeryEffective = "not_very_effective";
    public const string SuperEffective = "super_effective";

    // Daño y curacion
    public const string DamageDealt = "damage_dealt";
    public const string HpRestored = "hp_restored";
    public const string Recoil = "recoil";
    public const string DrainHp = "drain_hp";

    // Estados primarios
    public const string Poisoned = "poisoned";
    public const string BadlyPoisoned = "badly_poisoned";
    public const string Burned = "burned";
    public const string Paralyzed = "paralyzed";
    public const string Asleep = "asleep";
    public const string Frozen = "frozen";
    public const string StatusCured = "status_cured";

    // Impedimentos de estado
    public const string ParalyzedCantMove = "paralyzed_cant_move";
    public const string FastAsleep = "fast_asleep";
    public const string FrozenSolid = "frozen_solid";
    public const string WokeUp = "woke_up";
    public const string Thawed = "thawed";

    // Estados secundarios
    public const string ConfusionStart = "confusion_start";
    public const string ConfusionEnd = "confusion_end";
    public const string ConfusionSelfHit = "confusion_self_hit";
    public const string Flinched = "flinched";
    public const string Seeded = "seeded";
    public const string SeededDrain = "seeded_drain";
    public const string Bound = "bound";
    public const string BoundDamage = "bound_damage";

    // Efectos de fin de turno
    public const string PoisonDamage = "poison_damage";
    public const string BurnDamage = "burn_damage";

    // Cambios de estadisticas
    public const string StatRose = "stat_rose";
    public const string StatFell = "stat_fell";
    public const string StatMaxed = "stat_maxed";
    public const string StatMinned = "stat_minned";
    public const string StatsReset = "stats_reset";
    public const string StatProtected = "stat_protected";

    // Efectos de campo
    public const string LightScreenStart = "light_screen_start";
    public const string LightScreenEnd = "light_screen_end";
    public const string ReflectStart = "reflect_start";
    public const string ReflectEnd = "reflect_end";
    public const string MistStart = "mist_start";
    public const string MistEnd = "mist_end";

    // Cambios de Pokemon
    public const string SendOut = "send_out";
    public const string Withdraw = "withdraw";
    public const string ForcedSwitch = "forced_switch";
    public const string Fainted = "fainted";

    // Resultado de batalla
    public const string BattleWon = "battle_won";
    public const string BattleLost = "battle_lost";

    // PP y movimientos
    public const string OutOfPp = "out_of_pp";
    public const string NoMovesLeft = "no_moves_left";
}
