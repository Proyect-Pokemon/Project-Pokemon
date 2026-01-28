using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities;
public class Nature {
    public int Id { get; set; }
    public required PokeNature Name { get; set; }

    public required StatType StatBoost { get; set; } // En la lógica --> x1.1
    public required StatType StatDrop { get; set; } // En la lógica --> x0.9
    public ICollection<PokemonBattle> PokemonBattles { get; set; } = new List<PokemonBattle>();
}
