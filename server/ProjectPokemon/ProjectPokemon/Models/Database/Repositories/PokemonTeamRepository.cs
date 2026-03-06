using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database.Repositories; 
public class PokemonTeamRepository : BaseRepository<PokemonTeam, int> {
    public PokemonTeamRepository(PokemonDbContext context) : base(context) { }
}
