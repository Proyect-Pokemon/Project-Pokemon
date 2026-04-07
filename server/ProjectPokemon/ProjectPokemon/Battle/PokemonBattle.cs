using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Battle.Movements;

namespace ProjectPokemon.Battle;

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
    public int BaseAttack { get; private set; }
    public int BaseDefense { get; private set; }
    public int BaseSpecialAttack { get; private set; }
    public int BaseSpecialDefense { get; private set; }
    public int BaseSpeed { get; private set; }

    // Estadísticas actuales durante el combate
    public int CurrentHp { get; set; }
    public int CurrentAttack { get; set; }
    public int CurrentDefense { get; set; }
    public int CurrentSpecialAttack { get; set; }
    public int CurrentSpecialDefense { get; set; }
    public int CurrentSpeed { get; set; }

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

        // Inicializar estadísticas actuales
        ResetCurrentStats();

        // Convertir movimientos usando el factory para crear el tipo correcto
        Movements = new List<BattleMovement>();
        if (pokemonTeam.Movement1 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement1));
        if (pokemonTeam.Movement2 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement2));
        if (pokemonTeam.Movement3 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement3));
        if (pokemonTeam.Movement4 != null) Movements.Add(BattleMovementFactory.Create(pokemonTeam.Movement4));
    }

    // Comprueba qué las estadísticas tienen que modificarse por la naturaleza.
    private void CalculateBaseStats(Pokemon pokemon, Nature nature) {
        // HP no se ve afectado por Nature
        MaxHp = pokemon.Hp;

        // Aplicar modificadores de Nature a las demás stats
        BaseAttack = ApplyNatureModifier(pokemon.Attack, nature.StatBoost, nature.StatDrop, StatType.Attack);
        BaseDefense = ApplyNatureModifier(pokemon.Defense, nature.StatBoost, nature.StatDrop, StatType.Defense);
        BaseSpecialAttack = ApplyNatureModifier(pokemon.SpecialAttack, nature.StatBoost, nature.StatDrop, StatType.SpecialAttack);
        BaseSpecialDefense = ApplyNatureModifier(pokemon.SpecialDefense, nature.StatBoost, nature.StatDrop, StatType.SpecialDefense);
        BaseSpeed = ApplyNatureModifier(pokemon.Speed, nature.StatBoost, nature.StatDrop, StatType.Speed);
    }

    // Aplica el modificador de la naturaleza a la estadística correspondiente. +10% o -10%
    private int ApplyNatureModifier(int baseStat, StatType boosted, StatType dropped, StatType currentStat) {
        double modifier = 1.0;

        if (boosted == currentStat) {
            modifier = 1.1; // +10%
        }
        else if (dropped == currentStat) {
            modifier = 0.9; // -10%
        }

        return (int)Math.Floor(baseStat * modifier);
    }

    // Reinicia las estadísticas del pokemon a sus valores base.
    // Por ejemplo, se usaría cuando el pokemon sale del combate
    private void ResetCurrentStats() {
        CurrentHp = MaxHp;
        CurrentAttack = BaseAttack;
        CurrentDefense = BaseDefense;
        CurrentSpecialAttack = BaseSpecialAttack;
        CurrentSpecialDefense = BaseSpecialDefense;
        CurrentSpeed = BaseSpeed;
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
}
