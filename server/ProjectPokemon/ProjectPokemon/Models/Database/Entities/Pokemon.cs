using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities; 
public class Pokemon {
    public int Id { get; set; }
    // Estadísticas
    public required int Hp { get; set; }
    public required int Atk { get; set; }
    public required int Def { get; set; }
    public required int SpAtk { get; set; }
    public required int SpDef { get; set; }
    public required int Spe {  get; set; }
    public required float Weight { get; set; }
    // Sprites y Sonidos
    public required string SpriteFront { get; set; }
    public required string SpriteBack { get; set; }
    public string? SpriteFrontShiny { get; set; } // <-- Será required
    public string? SpriteBackShiny { get; set; } // <-- Será required
    public string? SpriteFrontFem { get; set; }
    public string? SpriteBackFem { get; set; }
    public string? SpriteFrontFemShiny { get; set; }
    public string? SpriteBackFemShiny { get; set; }
    public string? Cry { get; set; } // <-- Será required

    // Relaciones
    public required PokemonType Type1 { get; set; }
    public PokemonType? Type2 { get; set; }
    public ICollection<Movement> Movevements { get; set; } = new List<Movement>();
}
