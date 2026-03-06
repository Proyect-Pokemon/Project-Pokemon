using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities; 
public class Pokemon {
    public int Id { get; set; }
    public required string Name { get; set; }
    // Estadísticas
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed {  get; set; }
    public float Weight { get; set; }
    // Sprites y Sonidos
    public required string SpriteFront { get; set; }
    public required string SpriteBack { get; set; }
    public required string SpriteFrontShiny { get; set; }
    public required string SpriteBackShiny { get; set; }
    public string? SpriteFrontFem { get; set; }
    public string? SpriteBackFem { get; set; }
    public string? SpriteFrontFemShiny { get; set; }
    public string? SpriteBackFemShiny { get; set; }
    public required string MiniSprite { get; set; }
    public string? Cry { get; set; } // <-- Será required

    // Relaciones
    public required PokeType Type1 { get; set; }
    public PokeType? Type2 { get; set; }
    public ICollection<PokemonMovement> PokemonMovements { get; set; } = new List<PokemonMovement>();
    public ICollection<PokemonBattle> PokemonBattles { get; set; } = new List<PokemonBattle>();
    public ICollection<PokemonTeam> PokemonTeams { get; set; } = new List<PokemonTeam>();
}
