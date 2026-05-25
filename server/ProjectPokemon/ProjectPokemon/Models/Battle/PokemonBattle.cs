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
    public int Slot { get; set; }
    public string SpriteFront { get; private set; }
    public string SpriteBack { get; private set; }
    public string SpriteFrontShiny { get; private set; }
    public string SpriteBackShiny { get; private set; }
    public string? SpriteFrontFem { get; private set; }
    public string? SpriteBackFem { get; private set; }
    public string? SpriteFrontFemShiny { get; private set; }
    public string? SpriteBackFemShiny { get; private set; }

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

    // Contador de turnos de sueño (Sleep)
    // Cuando Status == Sleep, indica cuántos turnos quedan dormido
    public int SleepTurnsRemaining { get; set; } = 0;

    // Contador de daño incremental de BadlyPoisoned
    // Comienza en 1 y se incrementa cada turno (1/16, 2/16, 3/16...)
    // Se resetea a 1 cuando el Pokémon es cambiado
    public int BadlyPoisonedCounter { get; set; } = 1;

    // Contador de turnos de confusión
    // Cuando SecondaryStatuses tiene Confuse, indica cuántos turnos quedan confundido (1-4)
    public int ConfusionTurnsRemaining { get; set; } = 0;

    // Referencia al Pokémon que aplicó Leech Seed
    // Null si no está afectado por Seeded
    public PokemonBattle? LeechSeedSource { get; set; } = null;

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
        Slot = pokemonTeam.Slot;
        SpriteFrontShiny = pokemonTeam.Pokemon.SpriteFrontShiny;
        SpriteBackShiny = pokemonTeam.Pokemon.SpriteBackShiny;
        SpriteFrontFem = pokemonTeam.Pokemon.SpriteFrontFem;
        SpriteBackFem = pokemonTeam.Pokemon.SpriteBackFem;
        SpriteFrontFemShiny = pokemonTeam.Pokemon.SpriteFrontFemShiny;
        SpriteBackFemShiny = pokemonTeam.Pokemon.SpriteBackFemShiny;
        SpriteFront = ResolveSpriteFront(pokemonTeam.Pokemon, pokemonTeam.Shiny, pokemonTeam.Sex);
        SpriteBack = ResolveSpriteBack(pokemonTeam.Pokemon, pokemonTeam.Shiny, pokemonTeam.Sex);
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

    private static string ResolveSpriteFront(Pokemon pokemon, bool shiny, char? sex) {
        bool female = sex.HasValue && (char.ToLowerInvariant(sex.Value) == 'h' || char.ToLowerInvariant(sex.Value) == 'f');

        if (shiny) {
            if (female && !string.IsNullOrWhiteSpace(pokemon.SpriteFrontFemShiny)) return pokemon.SpriteFrontFemShiny;
            return pokemon.SpriteFrontShiny;
        }

        if (female && !string.IsNullOrWhiteSpace(pokemon.SpriteFrontFem)) return pokemon.SpriteFrontFem;
        return pokemon.SpriteFront;
    }

    private static string ResolveSpriteBack(Pokemon pokemon, bool shiny, char? sex) {
        bool female = sex.HasValue && (char.ToLowerInvariant(sex.Value) == 'h' || char.ToLowerInvariant(sex.Value) == 'f');

        if (shiny) {
            if (female && !string.IsNullOrWhiteSpace(pokemon.SpriteBackFemShiny)) return pokemon.SpriteBackFemShiny;
            return pokemon.SpriteBackShiny;
        }

        if (female && !string.IsNullOrWhiteSpace(pokemon.SpriteBackFem)) return pokemon.SpriteBackFem;
        return pokemon.SpriteBack;
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
        } else if (boosted == currentStat) {
            return 1.1; // +10%
        } else if (dropped == currentStat) {
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

    // Intenta descongelar al Pokémon (20% de probabilidad)
    // Devuelve true si se descongela, false si sigue congelado
    public bool TryThawOut() {
        if (Status != PokeStatus.Freeze) {
            return false; // No está congelado
        }

        Random random = new Random();
        if (random.Next(0, 100) < 20) { // 20% de probabilidad
            Status = PokeStatus.None;
            return true; // Se descongeló
        }

        return false; // Sigue congelado
    }

    // Verifica si el Pokémon puede atacar considerando su estado
    // Devuelve (canAttack, message)
    public (bool canAttack, string? message) CanAttack() {
        // Procesar confusión (estado secundario) primero
        if (HasSecondaryStatus(PokeSecondaryStatus.Confuse)) {
            ConfusionTurnsRemaining--;

            if (ConfusionTurnsRemaining <= 0) {
                // Se cura la confusión
                RemoveSecondaryStatus(PokeSecondaryStatus.Confuse);
                ConfusionTurnsRemaining = 0;
                return (true, $"{GetDisplayName()} ya no está confundido.");
            }

            // 50% de probabilidad de golpearse a sí mismo
            Random random = new Random();
            if (random.Next(0, 100) < 50) {
                // Se golpea a sí mismo: daño = movimiento de 40 de potencia
                // Fórmula simplificada: ((2 * 50 / 5 + 2) * 40 * Atk / Def) / 50
                int attack = GetModifiedStat(StatType.Attack);
                int defense = GetModifiedStat(StatType.Defense);
                int damage = Math.Max(1, ((2 * 50 / 5 + 2) * 40 * attack / defense) / 50);
                TakeDamage(damage);
                return (false, $"{GetDisplayName()} está confundido y se golpeó a sí mismo por {damage} PS.");
            }
        }

        // Procesar estados primarios
        switch (Status) {
            case PokeStatus.Freeze:
                // Intentar descongelarse antes de atacar
                if (TryThawOut()) {
                    return (true, $"{GetDisplayName()} se ha descongelado y puede atacar.");
                }
                return (false, $"{GetDisplayName()} está congelado y no puede moverse.");

            case PokeStatus.Paralysis:
                // 25% de probabilidad de no poder atacar debido a la parálisis
                Random random = new Random();
                if (random.Next(0, 100) < 25) {
                    return (false, $"{GetDisplayName()} está paralizado y no puede moverse.");
                }
                return (true, null); // Puede atacar (75% de las veces)

            case PokeStatus.Sleep:
                // Decrementar turnos de sueño
                SleepTurnsRemaining--;

                if (SleepTurnsRemaining <= 0) {
                    // Se despierta y puede atacar este turno
                    Status = PokeStatus.None;
                    SleepTurnsRemaining = 0;
                    return (true, $"{GetDisplayName()} se ha despertado y puede atacar.");
                }

                // Sigue dormido
                return (false, $"{GetDisplayName()} está dormido y no puede moverse.");

            default:
                return (true, null); // Puede atacar normalmente
        }
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

        double finalStat = baseStat * GetStatStageMultiplier(stage);

        // Aplicar efectos de estados alterados
        // Burn reduce el ataque físico a la mitad
        if (stat == StatType.Attack && Status == PokeStatus.Burn) {
            finalStat *= 0.5;
        }

        // Paralysis reduce la velocidad en un 75% (deja la velocidad al 25%)
        if (stat == StatType.Speed && Status == PokeStatus.Paralysis) {
            finalStat *= 0.25;
        }

        return (int)Math.Floor(finalStat);
    }

    // Aplica los efectos de estado al final del turno (solo si el Pokémon está activo en el campo)
    // Devuelve un mensaje describiendo el efecto, o null si no hay efecto
    public string? ApplyEndOfTurnStatusEffect() {
        // Solo aplica efectos si el Pokémon está activo (no debilitado)
        if (IsFainted()) {
            return null;
        }

        switch (Status) {
            case PokeStatus.Burn:
                // Burn: pierde 1/16 de sus PS máximos
                int burnDamage = Math.Max(1, MaxHp / 16);
                TakeDamage(burnDamage);
                return $"{GetDisplayName()} sufre daño por quemadura ({burnDamage} PS).";

            case PokeStatus.Poison:
                // Poison: pierde 1/8 de sus PS máximos
                int poisonDamage = Math.Max(1, MaxHp / 8);
                TakeDamage(poisonDamage);
                return $"{GetDisplayName()} sufre daño por envenenamiento ({poisonDamage} PS).";

            case PokeStatus.BadlyPoisoned:
                // BadlyPoisoned: pierde N/16 de sus PS máximos (N = BadlyPoisonedCounter)
                // El daño incrementa cada turno: 1/16, 2/16, 3/16, etc.
                int toxicDamage = Math.Max(1, (MaxHp * BadlyPoisonedCounter) / 16);
                TakeDamage(toxicDamage);
                string message = $"{GetDisplayName()} sufre daño por envenenamiento grave ({toxicDamage} PS).";

                // Incrementar el contador para el siguiente turno
                BadlyPoisonedCounter++;

                return message;

            default:
                return null;
        }
    }

    // Aplica los efectos de estados secundarios al final del turno
    // Devuelve un mensaje describiendo el efecto, o null si no hay efecto
    public string? ApplyEndOfTurnSecondaryStatusEffect() {
        // Solo aplica efectos si el Pokémon está activo (no debilitado)
        if (IsFainted()) {
            return null;
        }

        // Leech Seed: pierde 1/8 de sus PS máximos y el atacante original los recupera
        if (HasSecondaryStatus(PokeSecondaryStatus.Seeded) && LeechSeedSource != null) {
            int drainedHp = Math.Max(1, MaxHp / 8);
            TakeDamage(drainedHp);

            // Curar al Pokémon que aplicó Leech Seed (si sigue vivo)
            if (!LeechSeedSource.IsFainted()) {
                LeechSeedSource.Heal(drainedHp);
                return $"{GetDisplayName()} pierde {drainedHp} PS por Drenadoras. {LeechSeedSource.GetDisplayName()} recupera {drainedHp} PS.";
            }

            return $"{GetDisplayName()} pierde {drainedHp} PS por Drenadoras.";
        }

        return null;
    }
}