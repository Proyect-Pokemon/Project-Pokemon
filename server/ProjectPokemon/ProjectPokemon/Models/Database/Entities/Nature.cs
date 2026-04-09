using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities;
public class Nature {
    public int Id { get; set; }
    public required PokeNature Name { get; set; }

    public required StatType StatBoost { get; set; } // En la lógica --> x1.1
    public required StatType StatDrop { get; set; } // En la lógica --> x0.9
    public ICollection<PokemonTeam> PokemonTeams { get; set; } = new List<PokemonTeam>();
}
