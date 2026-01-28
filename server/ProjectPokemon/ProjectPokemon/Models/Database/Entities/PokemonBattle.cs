using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities; 
public class PokemonBattle {
    public int Id { get; set; }
    public required int Team { get; set; }
    public bool Shiny { get; set; } = false;
    public int PokemonId { get; set; }
    // public int IdBattle { get; set; } // FK de Battle
    public int NatureId { get; set; }
    public int MovementId1 { get; set; }
    public int MovementId2 { get; set; }
    public int MovementId3 { get; set; }
    public int MovementId4 { get; set; }
    public PokeStatus Status { get; set; } = PokeStatus.None;
    public Movement Movement1 { get; set; } = null!;
    public Movement Movement2 { get; set; } = null!;
    public Movement Movement3 { get; set; } = null!;
    public Movement Movement4 { get; set; } = null!;
    // public Battle Battle { get; set; }
    public Pokemon Pokemon { get; set; } = null!;
    public Nature Nature { get; set; } = null!;
}
