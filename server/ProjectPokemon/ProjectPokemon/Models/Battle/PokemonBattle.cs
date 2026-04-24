using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Battle.Movements;

namespace ProjectPokemon.Models.Battle;

/// <summary>
/// Representa un Pokémon durante el combate activo.
/// Se construye desde PokemonTeam y mantiene el estado temporal del combate.
/// </summary>
public class PokemonBattle {
    // Información básica del Pokémon
    public int PokemonId { get; private set; }
    public string Name { get; private set; }
    public string? Nickname { get; private set; }
    public PokeType Type1 { get; private set; }
    public PokeType? Type2 { get; private set; }
    public bool Shiny { get; private set; }
    public char? Sex { get; private set; }

    // Estadísticas base con la naturaleza aplicada
    public int MaxHp { get; private set; }
    public int CurrentHp { get; set; }
    public int BaseAttack { get; private set; }
    public int BaseDefense { get; private set; }
    public int BaseSpecialAttack { get; private set; }
    public int BaseSpecialDefense { get; private set; }
    public int BaseSpeed { get; private set; }

    // Stages de modificación temporal (-6 a +6, se resetean al cambiar de Pokémon)
    public int AttackStage { get; set; }
    public int DefenseStage { get; set; }
    public int SpecialAttackStage { get; set; }
    public int SpecialDefenseStage { get; set; }
    public int SpeedStage { get; set; }
    public int AccuracyStage { get; set; }
    public int EvasionStage { get; set; }

    // Estado alterado actual
    public PokeStatus Status { get; set; }
    // Estados secundarios (puede tener varios a la vez usando flags)
    public PokeSecondaryStatus SecondaryStatuses { get; set; }

    // Movimientos disponibles en el combate
    public List<BattleMovement> Movements { get; private set; }

    // Constructor desde PokemonTeam
    public PokemonBattle(PokemonTeam pokemonTeam) {
        // Información básica
        PokemonId = pokemonTeam.PokemonId;
        Name = pokemonTeam.Pokemon.Name;
        Nickname = pokemonTeam.Nickname;
        Type1 = pokemonTeam.Pokemon.Type1;
        Type2 = pokemonTeam.Pokemon.Type2;
        Shiny = pokemonTeam.Shiny;
        Sex = pokemonTeam.Sex;
        Status = PokeStatus.None;
        SecondaryStatuses = PokeSecondaryStatus.None;

        // Calcular estadísticas base aplicando Nature
        CalculateBaseStats(pokemonTeam.Pokemon, pokemonTeam.Nature);

        // Inicializar HP y stages
        InitializeBattleStats();

        // Convertir movimientos usando el factory para crear el tipo correcto
        Movements = new List<BattleMovement>();
        if (pokemonTeam.Movement1 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement1));
        if (pokemonTeam.Movement2 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement2));
        if (pokemonTeam.Movement3 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement3));
        if (pokemonTeam.Movement4 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement4));
    }

    // Calcula las estadísticas del Pokémon
    private void CalculateBaseStats(Pokemon pokemon, Nature nature) {
        // Valores por defecto para competitivo
        int level = 50;  // Nivel estándar competitivo
        int iv = 31;     // IVs máximos
        int ev = 0;      // Sin EVs (o podría ser 252)

        // Fórmula para calcular la vida
        // HP = floor(((2 * Base) + IV + floor(EV/4)) * Nivel / 100) + Nivel + 10
        MaxHp = (int)Math.Floor(((2 * pokemon.Hp + iv + (ev / 4.0)) * level) / 100) + level + 10;

        // Fórmula para el resto de stats
        // Stat = floor((floor(((2 * Base) + IV + floor(EV/4)) * Nivel / 100) + 5) * Naturaleza)
        BaseAttack = CalculateStat(pokemon.Attack, level, iv, ev, nature.StatBoost, nature.StatDrop, StatType.Attack);
        BaseDefense = CalculateStat(pokemon.Defense, level, iv, ev, nature.StatBoost, nature.StatDrop, StatType.Defense);
        BaseSpecialAttack = CalculateStat(pokemon.SpecialAttack, level, iv, ev, nature.StatBoost, nature.StatDrop, StatType.SpecialAttack);
        BaseSpecialDefense = CalculateStat(pokemon.SpecialDefense, level, iv, ev, nature.StatBoost, nature.StatDrop, StatType.SpecialDefense);
        BaseSpeed = CalculateStat(pokemon.Speed, level, iv, ev, nature.StatBoost, nature.StatDrop, StatType.Speed);
    }

    // Calcula una estadística usando la fórmula oficial de Pokémon
    private int CalculateStat(int baseStat, int level, int iv, int ev, StatType natureBoosted, StatType natureDropped, StatType currentStat) {
        // Cálculo base: floor(((2 × Base) + IV + floor(EV/4)) × Nivel / 100) + 5
        int statValue = (int)Math.Floor(((2 * baseStat + iv + (ev / 4.0)) * level) / 100) + 5;

        // Aplicar modificador de naturaleza
        double natureModifier = GetNatureModifier(natureBoosted, natureDropped, currentStat);

        return (int)Math.Floor(statValue * natureModifier);
    }

    // Obtiene el modificador de naturaleza para una estadística
    private double GetNatureModifier(StatType boosted, StatType dropped, StatType currentStat) {
        if (boosted == dropped) {
            return 1.0; // Naturaleza neutra (sube y baja la misma stat)
        }
        else if (boosted == currentStat) {
            return 1.1; // +10%
        }
        else if (dropped == currentStat) {
            return 0.9; // -10%
        }
        return 1.0; // Debe tener un return por si ninguno de los anteriores funciona
    }

    // Inicializa el HP y los stages del Pokémon al entrar en combate
    private void InitializeBattleStats() {
        CurrentHp = MaxHp;
        ResetStages();
    }

    // Reinicia todos los stages a 0
    public void ResetStages() {
        AttackStage = 0;
        DefenseStage = 0;
        SpecialAttackStage = 0;
        SpecialDefenseStage = 0;
        SpeedStage = 0;
        AccuracyStage = 0;
        EvasionStage = 0;
    }

    // Cálculo del daño que recibe el pokemon
    public void TakeDamage(int damage) {
        CurrentHp -= damage;
        if (CurrentHp < 0) {
            CurrentHp = 0;
        }
    }

    // Comprueba si el pokemon es debilitado
    public bool IsFainted() {
        return CurrentHp <= 0;
    }

    // Cura al pokemon
    public void Heal(int amount) {
        CurrentHp += amount;
        if (CurrentHp > MaxHp) {
            CurrentHp = MaxHp;
        }
    }

    public BattleMovement? GetMovement(int index) {
        if (index < 0 || index >= Movements.Count) {
            return null;
        }
        return Movements[index];
    }

    // Muestra el nickname si tiene, sino, su nombre
    public string GetDisplayName() {
        return !string.IsNullOrWhiteSpace(Nickname) ? Nickname : Name;
    }

    // Métodos para los estados secundarios usando operaciones de bits
    public void AddSecondaryStatus(PokeSecondaryStatus status) {
        SecondaryStatuses |= status;
    }

    public void RemoveSecondaryStatus(PokeSecondaryStatus status) {
        SecondaryStatuses &= ~status;
    }

    public bool HasSecondaryStatus(PokeSecondaryStatus status) {
        return (SecondaryStatuses & status) != 0;
    }

    public void ClearSecondaryStatuses() {
        SecondaryStatuses = PokeSecondaryStatus.None;
    }

    // Modifica un stage específico (limitado entre -6 y +6)
    public void ModifyStage(StatType stat, int change) {
        switch (stat) {
            case StatType.Attack:
                AttackStage = Math.Clamp(AttackStage + change, -6, 6);
                break;
            case StatType.Defense:
                DefenseStage = Math.Clamp(DefenseStage + change, -6, 6);
                break;
            case StatType.SpecialAttack:
                SpecialAttackStage = Math.Clamp(SpecialAttackStage + change, -6, 6);
                break;
            case StatType.SpecialDefense:
                SpecialDefenseStage = Math.Clamp(SpecialDefenseStage + change, -6, 6);
                break;
            case StatType.Speed:
                SpeedStage = Math.Clamp(SpeedStage + change, -6, 6);
                break;
            case StatType.Accuracy:
                AccuracyStage = Math.Clamp(AccuracyStage + change, -6, 6);
                break;
            case StatType.Evasion:
                EvasionStage = Math.Clamp(EvasionStage + change, -6, 6);
                break;
        }
    }

    // Obtiene el multiplicador de stage para estadísticas normales (-6 a +6)
    // Attack, Defense, SpecialAttack, SpecialDefense, Speed
    public static double GetStatStageMultiplier(int stage) {
        stage = Math.Clamp(stage, -6, 6);

        return stage switch {
            -6 => 0.25,
            -5 => 0.29,
            -4 => 0.33,
            -3 => 0.4,
            -2 => 0.5,
            -1 => 0.67,
            0 => 1.0,
            1 => 1.5,
            2 => 2.0,
            3 => 2.5,
            4 => 3.0,
            5 => 3.5,
            6 => 4.0,
            _ => 1.0
        };
    }

    // Obtiene el multiplicador de stage para Accuracy/Evasion (-6 a +6)
    public static double GetAccuracyEvasionStageMultiplier(int stage) {
        stage = Math.Clamp(stage, -6, 6);

        return stage switch {
            -6 => 0.33,
            -5 => 0.38,
            -4 => 0.43,
            -3 => 0.5,
            -2 => 0.6,
            -1 => 0.75,
            0 => 1.0,
            1 => 1.33,
            2 => 1.67,
            3 => 2.0,
            4 => 2.33,
            5 => 2.67,
            6 => 3.0,
            _ => 1.0
        };
    }

    // Obtiene el valor real de una estadística aplicando sus modificadores (stages)
    public int GetModifiedStat(StatType stat) {
        int baseStat = stat switch {
            StatType.Attack => BaseAttack,
            StatType.Defense => BaseDefense,
            StatType.SpecialAttack => BaseSpecialAttack,
            StatType.SpecialDefense => BaseSpecialDefense,
            StatType.Speed => BaseSpeed,
            _ => throw new ArgumentException($"No se puede obtener stat real de {stat}")
        };

        int stage = stat switch {
            StatType.Attack => AttackStage,
            StatType.Defense => DefenseStage,
            StatType.SpecialAttack => SpecialAttackStage,
            StatType.SpecialDefense => SpecialDefenseStage,
            StatType.Speed => SpeedStage,
            _ => 0
        };

        return (int)Math.Floor(baseStat * GetStatStageMultiplier(stage));
    }
}
