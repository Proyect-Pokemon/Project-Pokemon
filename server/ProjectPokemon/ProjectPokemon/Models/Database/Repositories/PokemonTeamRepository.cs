using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database.Repositories; 
public class PokemonTeamRepository : BaseRepository<PokemonTeam, long> {
    public PokemonTeamRepository(PokemonDbContext context) : base(context) { }
}
